# 🤖 SoftwareZator AI Controller Context

## 👋 Hello AI!
You are currently looking at the **runtime environment** (bin folder) of **SoftwareZator 2012**.
Your goal is to assist the user by controlling this application. You do NOT need the source code. You control the app directly via the **communication pipe**.

## 🔌 How to Control the App
To execute commands (like creating forms, adding buttons, etc.), you must WRITE a JSON object to the file:
`AI_Pipe.json`

### 📝 Protocol Format
Write a JSON object with at least a `script` field containing the valid command syntax.
The `script` field is a string representation of a JSON list of actions.

**Example of Valid Write:**
```json
{
  "status": "COMPLETED",
  "message": "I have created a contact form for you.",
  "script": "[{'action':'CREATE_FORM','name':'frmContact'}, {'action':'ADD_CONTROL','type':'TextBox','text':'Email','location':'20 20'}]"
}
```

### 🛠 Supported Commands (The "Script")
Inside the `script` string, you can use these actions:

1.  **CREATE_PROJECT**
    *   `{'action':'CREATE_PROJECT', 'name':'MyProject', 'type':'Window'}`
    *   *Creates a new project from template.*

2.  **CREATE_FORM**
    *   `{'action':'CREATE_FORM', 'name':'FormName'}`
    *   *Creates a new window/form.*

3.  **ADD_CONTROL**
    *   `{'action':'ADD_CONTROL', 'type':'Button', 'text':'Click Me', 'location':'x y', 'size':'w h'}`
    *   *Supported Types*: `Button`, `Label`, `TextBox`, `CheckBox`, `RichTextBox`, `Panel`

## 🧠 Your Role
1.  **Read User Request**: The user might drop a request in `AI_Pipe.json` (status: `PENDING`) or ask you directly in your chat interface.
2.  **Generate Plan**: Decide which commands are needed.
3.  **Execute**: Write the JSON to `AI_Pipe.json`.
4.  **Confirm**: Tell the user "Done! Check the simulator."

---
*This file (`AI_CONTEXT.md`) allows any AI model to instantly understand and control SoftwareZator just by being in this folder.*
