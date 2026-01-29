# Custom Instructions for SoftwareZator Control

You are working with **SoftwareZator 2012** runtime environment.

## Critical Files

- **`AI_CONTEXT.md`**: Complete documentation (READ THIS FIRST)
- **`AI_Pipe.json`**: Communication channel (WRITE COMMANDS HERE)

## Your Capabilities

You can:

1. Create new forms/windows
2. Add UI controls (buttons, textboxes, grids, etc.)
3. **Modify properties of existing controls** (Text, Location, Size, RightToLeft, etc.)
4. **Delete controls** from the designer
5. **Open existing forms** in the designer
6. **Get a list of all controls** on the current form
7. Inject VB.NET code into event handlers
8. Create database tables
9. Bind data to controls

## Command Format

Write JSON to `AI_Pipe.json`:

```json
{
  "status": "COMPLETED",
  "message": "Description of what you're doing",
  "script": [
    {"action": "ACTION_NAME", "param": "value"}
  ]
}
```

## Available Actions (10 Total)

| Action | Purpose |
|--------|---------|
| `CREATE_PROJECT` | New project from template |
| `CREATE_FORM` | New window/form |
| `ADD_CONTROL` | UI control (Button, Label, TextBox, DataGridView, etc.) |
| `ADD_CODE` | VB.NET code injection |
| `CREATE_TABLE` | Database table (Access) |
| `BIND_DATA` | Data binding |
| `SET_PROPERTY` | Modify control properties |
| `DELETE_CONTROL` | Remove a control |
| `OPEN_FORM` | Open existing form |
| `GET_CONTROLS` | Get all controls list |

## New Commands

### OPEN_FORM - Open Existing Form

```json
{"action": "OPEN_FORM", "name": "Form2"}
```

### GET_CONTROLS - Get Controls List

```json
{"action": "GET_CONTROLS"}
```

Response is written to `AI_Pipe.json` with control names, types, and properties.

## Prerequisites

1. SoftwareZator must be running
2. A project must be open
3. For database: `.mdb` or `.accdb` file in project folder

## First Step

**Always read `AI_CONTEXT.md`** for complete protocol details before executing commands.

---

*Last Updated: 2026-01-29*
