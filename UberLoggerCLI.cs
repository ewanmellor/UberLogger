using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class UberLoggerCLI : MonoBehaviour {

    const string CallbackMethodName = "UberLoggerCLI_HandleCommand";

    readonly Dictionary<string, Component> handlers = new Dictionary<string, Component>();
    string commandText = "";

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

    internal void DrawCLI()
    {
        if (!enabled)
        {
            return;
        }

        GUILayout.BeginHorizontal();
        UberLoggerAppWindow.LabelClamped("Command line", GUI.skin.label);
        if (Event.current.type == EventType.KeyDown &&
            Event.current.keyCode == KeyCode.Return)
        {
            ExecuteCommand(commandText);
            commandText = "";
        }
        commandText = GUILayout.TextArea(commandText);
        GUILayout.EndHorizontal();
    }

    void ExecuteCommand(string cmd)
    {
        var bits = cmd.Split(' ');
        var handlerName = bits[0];
        Component handler;
        if (handlers.TryGetValue(handlerName, out handler) && handler != null)
        {
            handler.SendMessage(CallbackMethodName, bits);
        }
        else
        {
            Debug.LogFormat("No such command: {0}", handlerName);
        }
    }
}
