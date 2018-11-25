using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;
using UnityEngine.Assertions;


public sealed class UberLoggerCLI : MonoBehaviour
{

    public const string TextFieldName = "UberLoggerCLI_TextField";
    const string CallbackMethodName = "UberLoggerCLI_HandleCommand";


    public interface IHandler
    {
        string UberLoggerCLI_CommandName { get; }

        /// <summary>
        /// May be null or empty, in which case this command is a leaf and
        /// uses UberLoggerCLI_Action instead.
        /// </summary>
        Dictionary<string, IHandler> UberLoggerCLI_Subcommands { get; }

        /// <summary>
        /// May be null, in which case this command has no direct action, only
        /// subcommands.
        /// </summary>
        Action<string[]> UberLoggerCLI_Action { get; }
    }


    public class Handler : IHandler
    {
        protected readonly string CommandName;

        protected readonly Dictionary<string, IHandler> Subcommands = new Dictionary<string, IHandler>();

        protected readonly Action<string[]> Action;

        public Handler(string cmdName, IEnumerable<Handler> subhandlers = null, Action<string[]> action = null)
        {
            CommandName = cmdName;
            Action = action;

            if (subhandlers != null)
            {
                foreach (var h in subhandlers)
                {
                    AddHandler(h);
                }
            }
        }

        public virtual string UberLoggerCLI_CommandName => CommandName;

        public virtual Dictionary<string, IHandler> UberLoggerCLI_Subcommands => Subcommands;

        public virtual Action<string[]> UberLoggerCLI_Action => Action;


        public void AddHandler(IHandler h)
        {
            Subcommands[h.UberLoggerCLI_CommandName] = h;
        }

        public void RemoveHandler(IHandler h)
        {
            Subcommands.Remove(h.UberLoggerCLI_CommandName);
        }

        public void RemoveHandlerByName(string n)
        {
            Subcommands.Remove(n);
        }
    }


    class ReflectiveHandler : IHandler
    {
        readonly object Object;
        readonly MethodInfo Method;

        internal ReflectiveHandler(object o, string cmdName)
        {
            Assert.IsNotNull(o);
            Assert.IsNotNull(cmdName);

            Object = o;
            UberLoggerCLI_CommandName = cmdName;

            var t = o.GetType();
            Method = t.GetMethod(CallbackMethodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(Method);
        }

        public string UberLoggerCLI_CommandName { get; }

        public Dictionary<string, IHandler> UberLoggerCLI_Subcommands => null;

        public Action<string[]> UberLoggerCLI_Action =>
            (string[] bits) => Method.Invoke(Object, new object[] { bits });
    }


    readonly Handler topHandler;
    string commandText = "";

    bool focusNextRedraw;


    UberLoggerCLI()
    {
        topHandler = new Handler("", null, _ => LogTopLevelCommands());
    }


    public void RegisterHandler(IHandler h)
    {
        topHandler.AddHandler(h);
    }

    public void UnregisterHandler(IHandler h)
    {
        topHandler.RemoveHandler(h);
    }

    public void RegisterHandler(string cmdName, object subcommands)
    {
        var handler = MakeHandler(cmdName, subcommands);
        topHandler.AddHandler(handler);
    }

    public void UnregisterHandler(string cmdName)
    {
        topHandler.RemoveHandlerByName(cmdName);
    }

    IHandler MakeHandler(string cmdName, object subcommands)
    {
        var action = subcommands as Action<string[]>;
        var dict = subcommands as Dictionary<string, object>;

        var result = new Handler(cmdName, null, action);
        if (dict != null)
        {
            foreach (var c in dict)
            {
                var h = MakeHandler(c.Key, c.Value);
                result.AddHandler(h);
            }
        }
        return result;
    }

    public void RegisterHandler(Component c, string cmdName = null)
    {
        cmdName = CommandName(c, cmdName);
        RegisterHandler(new ReflectiveHandler(c, cmdName));
    }

    public void UnregisterHandler(Component c, string cmdName = null)
    {
        cmdName = CommandName(c, cmdName);
        topHandler.RemoveHandlerByName(cmdName);
    }

    string CommandName(Component c, string cmdName)
    {
        return cmdName ?? c.name ?? c.GetType().Name;
    }


    public void FocusNextRedraw()
    {
        focusNextRedraw = true;
    }


    internal void DrawCLI()
    {
        if (!enabled)
        {
            return;
        }

        GUILayout.BeginHorizontal();
        UberLoggerAppWindow.LabelClamped("Command line", GUI.skin.label);

        var ev = Event.current;
        if ((ev.type == EventType.KeyDown || ev.type == EventType.KeyUp) &&
            ev.keyCode == KeyCode.Return)
        {
            ExecuteCommand();
            commandText = "";
            ev.Use();
        }
        GUI.SetNextControlName(TextFieldName);
        commandText = GUILayout.TextField(commandText);
        GUILayout.EndHorizontal();

        if (focusNextRedraw)
        {
            focusNextRedraw = false;
            GUI.FocusControl(TextFieldName);
        }
    }


    void ExecuteCommand()
    {
        var cmd = commandText.Trim();
        if (cmd == "")
        {
            return;
        }
        var bits = cmd.Split(' ');
        var idx = 0;
        IHandler handler = topHandler;
        while (true)
        {
            if (idx < bits.Length)
            {
                var handlerName = bits[idx];
                IHandler subhandler;
                var subcmds = handler.UberLoggerCLI_Subcommands;
                if (subcmds != null && subcmds.TryGetValue(handlerName, out subhandler))
                {
                    handler = subhandler;
                    idx++;
                    continue;
                }
            }

            var action = handler.UberLoggerCLI_Action;
            if (action == null)
            {
                LogHandlerUsage(handler);
            }
            else
            {
                action(bits);
            }
            return;
        }
    }


    void LogTopLevelCommands()
    {
        var commands = topHandler.UberLoggerCLI_Subcommands.Keys.OrderBy(x => x);
        Debug.LogFormat("Known commands: {0}", string.Join(", ", commands));
    }


    public static void LogHandlerUsage(IHandler handler)
    {
        var hs = handler.UberLoggerCLI_Subcommands;
        if (hs == null || hs.Count == 0)
        {
            Debug.LogFormat("Command {0} takes no arguments", handler.UberLoggerCLI_CommandName);
        }
        else
        {
            var subcommands = hs.Keys;
            LogSubcommandUsage(handler.UberLoggerCLI_CommandName, subcommands);
        }
    }


    public static void LogSubcommandUsage(string prefix, IEnumerable<string> commands)
    {
        Debug.LogFormat("Known subcommands of {0}: {1}", prefix, string.Join(", ", commands));
    }
}
