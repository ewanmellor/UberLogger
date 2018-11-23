using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;


public sealed class UberLoggerCLI : MonoBehaviour {

    public const string TextFieldName = "UberLoggerCLI_TextField";
    const string CallbackMethodName = "UberLoggerCLI_HandleCommand";

    readonly Dictionary<string, Component> handlers = new Dictionary<string, Component>();
    string commandText = "";

    bool focusNextRedraw;


    void Start()
    {
        RegisterHandler(this, "help");
    }


    public void RegisterHandler(Component c, string cmdName = null)
    {
        cmdName = CommandName(c, cmdName);
        handlers[cmdName] = c;
    }

    public void UnregisterHandler(Component c, string cmdName = null)
    {
        cmdName = CommandName(c, cmdName);
        handlers.Remove(cmdName);
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
        var handlerName = bits[0];
        Component handler;
        if (handlers.TryGetValue(handlerName, out handler) && handler != null)
        {
            var t = handler.GetType();
            var mi = t.GetMethod(CallbackMethodName, BindingFlags.NonPublic | BindingFlags.Instance);
            mi.Invoke(handler, new object[] { bits });
        }
        else
        {
            Debug.LogFormat("No such command handler: {0}", handlerName);
        }
    }


    void UberLoggerCLI_HandleCommand()
    {
        var handlerNames = handlers.Keys.OrderBy(x => x);
        LogCommandUsage(handlerNames);
    }


    public static void LogCommandUsage(IEnumerable<string> commands)
    {
        Debug.LogFormat("Known commands: {0}", string.Join(", ", commands));
    }


    public static void LogSubcommandUsage(string prefix, IEnumerable<string> commands)
    {
        Debug.LogFormat("Known subcommands of {0}: {1}", prefix, string.Join(", ", commands));
    }
}
