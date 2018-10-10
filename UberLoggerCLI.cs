using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class UberLoggerCLI : MonoBehaviour {

    public const string TextFieldName = "UberLoggerCLI_TextField";
    const string CallbackMethodName = "UberLoggerCLI_HandleCommand";

    readonly Dictionary<string, Component> handlers = new Dictionary<string, Component>();
    string commandText = "";

    bool focusNextRedraw;


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
            handler.SendMessage(CallbackMethodName, bits);
        }
        else
        {
            Debug.LogFormat("No such command handler: {0}", handlerName);
        }
    }
}
