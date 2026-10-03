# Schemas

These XSDs are SimCorp's, not mine. They came out of `ScdSvcCtl.exe` with

```powershell
ScdSvcCtl.exe exportschemas -dest:.\schemas -create
```

and describe the XML the tool writes when you run a command with `-format:xml`. They're here for reference, and because the tests check their sample XML against them. They aren't covered by this repo's licence.

The library models three of them: `ServiceList`, `AgentList` and `QueueitemList`. The rest are for commands that `ServiceControlClient.RunCommand` can already reach, and they could be turned into typed methods later.

After a Dimension upgrade, export them again over this folder and diff before touching any of the models. A changed `Version` attribute is exactly what the schema check will start rejecting.
