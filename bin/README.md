# SoftwareZator 2012 - Runtime Environment

> 🤖 **AI Assistant?** Read [`AI_CONTEXT.md`](./AI_CONTEXT.md) for complete instructions on how to control this application.

## Quick Start for AI

This folder contains a running instance of **SoftwareZator 2012** - a visual programming IDE.

**To control the application**, write JSON commands to `AI_Pipe.json`. See `AI_CONTEXT.md` for the complete protocol.

### Available Commands (10 Total)

| Command | Description |
|---------|-------------|
| `CREATE_PROJECT` | Create a new project |
| `CREATE_FORM` | Create a new form/window |
| `ADD_CONTROL` | Add UI control (Button, TextBox, etc.) |
| `ADD_CODE` | Inject VB.NET code |
| `CREATE_TABLE` | Create database table |
| `BIND_DATA` | Bind data to control |
| `SET_PROPERTY` | Modify existing control properties |
| `DELETE_CONTROL` | Remove a control |
| `OPEN_FORM` | **NEW** Open an existing form in designer |
| `GET_CONTROLS` | **NEW** Get list of controls on current form |

### Example Command

```json
{
  "status": "COMPLETED",
  "message": "Creating a login form",
  "script": [
    {"action": "CREATE_FORM", "name": "frmLogin"},
    {"action": "ADD_CONTROL", "type": "TextBox", "name": "txtUsername", "location": "20 20"},
    {"action": "ADD_CONTROL", "type": "Button", "name": "btnLogin", "text": "Login", "location": "20 60"},
    {"action": "SET_PROPERTY", "control": "frmLogin", "property": "RightToLeft", "value": "Yes"}
  ]
}
```

---

📚 **Full Documentation**: [AI_CONTEXT.md](./AI_CONTEXT.md)

*Last Updated: 2026-01-29*
