# 🤖 SoftwareZator AI Controller Context

> **Version**: 2.0  
> **Last Updated**: 2026-01-29

## 👋 Hello AI!

You are looking at the **runtime environment** (`bin` folder) of **SoftwareZator 2012** - a visual programming IDE similar to Visual Studio, but designed for beginners.

**Your Role**: You control this application **directly** via a JSON-based communication pipe. You do NOT need source code access. You generate commands, and the application executes them in real-time.

---

## 🔌 Communication Protocol

### The Pipe File
Write your commands to: **`AI_Pipe.json`** (same folder as this file)

### JSON Structure
```json
{
  "status": "COMPLETED",
  "message": "Human-readable description of what you did",
  "script": [
    {"action": "ACTION_NAME", "param1": "value1", ...},
    {"action": "ACTION_NAME", "param2": "value2", ...}
  ]
}
```

> ⚠️ **IMPORTANT**: The `script` field is a **JSON array**, not a string. Use proper JSON escaping (`\"` for quotes inside strings).

### Status Values
| Status | Meaning |
|--------|---------|
| `IDLE` | No pending commands (application resets to this after execution) |
| `COMPLETED` | You have written a command, waiting for app to pick it up |

---

## 🛠 Supported Actions

### 1. CREATE_PROJECT
Creates a new SoftwareZator project from a template.

```json
{"action": "CREATE_PROJECT", "name": "MyApp", "type": "Window"}
```

| Parameter | Required | Description |
|-----------|----------|-------------|
| `name` | ✅ | Project name (no spaces recommended) |
| `type` | ❌ | Template type: `Window` (default), `Console` |

---

### 2. CREATE_FORM
Creates a new window/form (`.szw` file) in the current project.

```json
{"action": "CREATE_FORM", "name": "frmProducts"}
```

| Parameter | Required | Description |
|-----------|----------|-------------|
| `name` | ✅ | Form name (becomes the filename) |

---

### 3. ADD_CONTROL
Adds a UI control to the currently active form in the designer.

```json
{"action": "ADD_CONTROL", "type": "Button", "name": "btnSave", "text": "Save", "location": "20 100", "size": "100 30"}
```

| Parameter | Required | Description |
|-----------|----------|-------------|
| `type` | ✅ | Control type (see list below) |
| `name` | ❌ | Unique identifier for the control |
| `text` | ❌ | Display text (for buttons, labels, etc.) |
| `location` | ❌ | Position as `"X Y"` (e.g., `"20 50"`) |
| `size` | ❌ | Dimensions as `"Width Height"` (e.g., `"200 30"`) |
| `parent` | ❌ | Name of parent control (for nesting in panels/menus) |

**Supported Control Types**:
- `Button`, `Label`, `TextBox`, `RichTextBox`
- `CheckBox`, `RadioButton`, `ComboBox`, `ListBox`
- `Panel`, `GroupBox`, `PictureBox`
- `DataGridView`, `KryptonDataGridView`
- `MenuStrip`, `ToolStripMenuItem` (for menus)
- `ProgressBar`, `TrackBar`, `NumericUpDown`
- `DateTimePicker`, `MonthCalendar`
- `TreeView`, `ListView`, `TabControl`

---

### 4. ADD_CODE
Injects VB.NET code into a control's event handler.

```json
{"action": "ADD_CODE", "control": "btnSave", "event": "Click", "code": "MessageBox.Show(\"Hello World\")"}
```

| Parameter | Required | Description |
|-----------|----------|-------------|
| `control` | ✅ | Name of the control to attach code to |
| `event` | ❌ | Event name (default: `Click`) |
| `code` | ✅ | VB.NET code to execute |

**Common Events**: `Click`, `Load`, `TextChanged`, `SelectedIndexChanged`, `KeyDown`

> ⚠️ **Escape quotes properly!** Use `\"` inside the code string.

---

### 5. CREATE_TABLE
Creates a database table. Requires an Access database file (`.mdb` or `.accdb`) in the project or solution folder.

```json
{"action": "CREATE_TABLE", "name": "Products", "columns": "Name,Price,Quantity", "types": "VARCHAR(100),DECIMAL,INT", "db_type": "Access"}
```

| Parameter | Required | Description |
|-----------|----------|-------------|
| `name` | ✅ | Table name |
| `columns` | ✅ | Comma-separated column names |
| `types` | ✅ | Comma-separated SQL types |
| `db_type` | ❌ | Database type: `Access` (default), `SQLServer`, `MySQL` |

**Notes**:
- An `Id` column (auto-increment primary key) is automatically added.
- For Access, ensure a `.mdb` or `.accdb` file exists in the project folder.

---

### 6. BIND_DATA
Generates code to load data from a database table into a control.

```json
{"action": "BIND_DATA", "control": "gridProducts", "table": "Products", "db_type": "Access"}
```

| Parameter | Required | Description |
|-----------|----------|-------------|
| `control` | ✅ | Name of the DataGridView control |
| `table` | ✅ | Table name to load |
| `db_type` | ❌ | Database type (default: `Access`) |

---

### 7. SET_PROPERTY
Modifies properties of existing controls (Text, Location, Size, RightToLeft, Color, etc.).

```json
{"action": "SET_PROPERTY", "control": "btnSave", "property": "Text", "value": "Save Changes"}
```

| Parameter | Required | Description |
|-----------|----------|-------------|
| `control` | ✅ | Name of the control to modify |
| `property` | ✅ | Property name (Text, Location, Size, Visible, etc.) |
| `value` | ✅ | New value for the property |

**Common Properties**:
- `Text` - Display text (string)
- `Location` - Position as `"X Y"` (e.g., `"100 200"`)
- `Size` - Dimensions as `"Width Height"` (e.g., `"200 50"`)
- `Visible` - Show/hide (`true` or `false`)
- `Enabled` - Enable/disable (`true` or `false`)
- `RightToLeft` - RTL support (`Yes`, `No`, `Inherit`)
- `RightToLeftLayout` - RTL layout for Forms (`True`, `False`)
- `BackColor` / `ForeColor` - Colors (`Red`, `Blue`, `#FF0000`)

---

### 8. DELETE_CONTROL
Removes a control from the designer.

```json
{"action": "DELETE_CONTROL", "control": "btnOld"}
```

| Parameter | Required | Description |
|-----------|----------|-------------|
| `control` | ✅ | Name of the control to delete |

---

### 9. OPEN_FORM
Opens an existing form in the designer. If the form is already open, it just activates it.

```json
{"action": "OPEN_FORM", "name": "Form2"}
```

| Parameter | Required | Description |
|-----------|----------|-------------|
| `name` | ✅ | Name of the form to open (without .szw extension) |

---

### 10. GET_CONTROLS
Retrieves a list of all controls on the current form. The response is written back to `AI_Pipe.json`.

```json
{"action": "GET_CONTROLS"}
```

**Response Format** (written to AI_Pipe.json):

```json
{
  "status": "CONTROLS_LIST",
  "form_name": "Form1",
  "count": 5,
  "controls": [
    {"name": "btnSave", "type": "Button", "text": "Save", "location": "20 50", "size": "100 30"},
    {"name": "txtName", "type": "TextBox", "text": "", "location": "20 100", "size": "200 25"}
  ]
}
```

---

## 📋 Complete Example: Point of Sale System

```json
{
  "status": "COMPLETED",
  "message": "Creating Point of Sale system with Products form and database",
  "script": [
    {"action": "CREATE_FORM", "name": "frmProducts"},
    {"action": "ADD_CONTROL", "type": "Label", "text": "Product Management", "location": "20 10"},
    {"action": "ADD_CONTROL", "type": "KryptonDataGridView", "name": "gridProducts", "location": "20 40", "size": "500 300"},
    {"action": "ADD_CONTROL", "type": "Button", "name": "btnAdd", "text": "Add Product", "location": "20 350"},
    {"action": "ADD_CONTROL", "type": "Button", "name": "btnDelete", "text": "Delete", "location": "130 350"},
    {"action": "CREATE_TABLE", "name": "Products", "columns": "ProductName,Price,Stock", "types": "VARCHAR(100),DECIMAL,INT", "db_type": "Access"},
    {"action": "ADD_CODE", "control": "btnAdd", "event": "Click", "code": "MessageBox.Show(\"Add product dialog\")"}
  ]
}
```

---

## 🧠 Workflow Summary

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│   User Request  │ ──▶ │   AI Generates   │ ──▶ │  AI_Pipe.json   │
│  (in chat)      │     │   JSON Script    │     │  (write here)   │
└─────────────────┘     └──────────────────┘     └────────┬────────┘
                                                          │
                                                          ▼
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  User sees      │ ◀── │  App Executes    │ ◀── │  App Polls      │
│  forms/controls │     │  Commands        │     │  (every 2 sec)  │
└─────────────────┘     └──────────────────┘     └─────────────────┘
```

---

## ⚠️ Prerequisites

1. **SoftwareZator must be running** with a project open.
2. **For database actions**: An Access database file (`.mdb` or `.accdb`) must exist in the project or solution folder.
3. **Forms must exist** before adding controls to them (use `CREATE_FORM` first).

---

## 🔍 Troubleshooting

| Problem | Solution |
|---------|----------|
| Nothing happens | Check if SoftwareZator is running and a project is open |
| "No Access database found" | Add a `.mdb` or `.accdb` file to your project folder |
| Controls not appearing | Ensure `CREATE_FORM` is called before `ADD_CONTROL` |
| JSON parse error | Check for proper quote escaping (`\"` not `""`) |

---

*This file enables any AI model to understand and control SoftwareZator instantly.*
