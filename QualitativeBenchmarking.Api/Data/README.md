# Dummy data file

When the API runs, benchmarking requests and users are stored in **`dummy-data.json`** in this folder (created automatically if it doesn’t exist).

- **First run:** The file is created with **sample data** (3 users, 3 sample benchmarking requests) so you can try the app immediately.
- **Later runs:** Data is loaded from this file.
- **Create/Update/Delete:** Every change is written back to this file immediately.

You can edit `dummy-data.json` by hand or add data only via the API. The path can be overridden in `Program.cs` or via `DummyData:FilePath` in appsettings.
