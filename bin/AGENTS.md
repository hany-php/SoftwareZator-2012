# SoftwareZator AI Agent Instructions

> **IMPORTANT**: This file provides context for AI agents working with SoftwareZator.

## Your Mission

You are an AI agent that can **control SoftwareZator 2012** - a visual programming IDE. You create forms, add controls, modify properties, and write code by sending JSON commands.

## How to Proceed

1. **Read the full documentation**: Open and read `AI_CONTEXT.md` in this folder
2. **Write commands to**: `AI_Pipe.json`
3. **Wait for execution**: The app polls every 2 seconds

## Quick Reference (10 Commands)

| Action | Purpose |
|--------|---------|
| `CREATE_PROJECT` | Create a new project |
| `CREATE_FORM` | Create a new window |
| `ADD_CONTROL` | Add UI element (Button, TextBox, etc.) |
| `ADD_CODE` | Inject VB.NET code into events |
| `CREATE_TABLE` | Create database table |
| `BIND_DATA` | Bind data to control |
| `SET_PROPERTY` | Modify existing control properties |
| `DELETE_CONTROL` | Remove a control from designer |
| `OPEN_FORM` | Open an existing form |
| `GET_CONTROLS` | Get list of all controls |

## New Commands

### OPEN_FORM

```json
{"action": "OPEN_FORM", "name": "Form2"}
```

Opens an existing form in the designer. If already open, it activates it.

### GET_CONTROLS

```json
{"action": "GET_CONTROLS"}
```

Returns a list of all controls on the current form. The response is written to `AI_Pipe.json`.

## Full Protocol

See **[AI_CONTEXT.md](./AI_CONTEXT.md)** for:

- Complete JSON format
- All supported control types
- All supported properties
- Database integration
- Troubleshooting guide

---

*Last Updated: 2026-01-29*
