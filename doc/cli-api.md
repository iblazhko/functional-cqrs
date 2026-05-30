# Interacting with the system using API

Assuming that the application is running as described in the
[“Build and Run”](./build-and-run.md) document, we can use either
`CQRS.CLI` project or a generic API client to interact with the system.

## CLI

In command prompt, navigate to `src/CQRS.CLI`. From there, you can run

```bash
dotnet run -- inventory <command> [arguments]
```

All inventory operations live under the `inventory` command group.

### Basic help

```bash
dotnet run -- --help
```

To list the inventory sub-commands:

```bash
dotnet run -- inventory --help
```

Available commands:

```txt
inventory create
inventory get
inventory add-items
inventory remove-items
inventory rename
inventory deactivate
```

### Command help

`dotnet run -- inventory <command> --help`, e.g.

```bash
dotnet run -- inventory create --help
```

### Create a new inventory

The inventory ID is auto-generated when `--id` is omitted. The name is
provided with `--name` (`-n`).

```bash
dotnet run -- inventory create --name "ABC-123"
```

Output (the generated `InventoryId`, followed by the command processing status):

```txt
InventoryId: 4IVPNNHXSOZI
Accepted. CommandId: 6605a0ed-edb1-4098-8f43-6de7361306d8
Completed. (CommandId: 6605a0ed-edb1-4098-8f43-6de7361306d8)
```

Note the `InventoryId` value (`4IVPNNHXSOZI` in the output above), you will need
it to provide input for the following commands.

### View current state of an inventory

The inventory ID is passed as a positional argument.

```bash
dotnet run -- inventory get 4IVPNNHXSOZI
```

Output:

```txt
Inventory:  4IVPNNHXSOZI
  Name:     ABC-123
  Stock:    0
  Active:   Yes
```

### Rename an inventory

```bash
dotnet run -- inventory rename 4IVPNNHXSOZI --name "ABC-456"
```

### Add items to an inventory

```bash
dotnet run -- inventory add-items 4IVPNNHXSOZI --count 5
```

### Remove items from an inventory

```bash
dotnet run -- inventory remove-items 4IVPNNHXSOZI --count 2
```

### Deactivate an inventory

Request will only be processed when the inventory is empty, i.e. has
`"stockQuantity": 0`. Once an inventory is deactivated, it will not be possible
to add/remove items or rename it.

```bash
dotnet run -- inventory deactivate 4IVPNNHXSOZI
```

## Using API Directly

Instead of using CLI you can call API endpoints directly using curl, Postman,
httpie etc. The command API accepts a command (returning `202 Accepted` with a
`commandId`) and the query API returns the current view model.

```bash
# Create an inventory (InventoryId is optional, auto-generated when omitted)
curl -X POST http://localhost:17322/inventories/ \
   -H 'Content-Type: application/json' \
   -d '{"Name": "ABC-123"}'

# Query current state
curl -X GET http://localhost:17322/inventories/4IVPNNHXSOZI

# Rename
curl -X PUT http://localhost:17322/inventories/4IVPNNHXSOZI/rename \
   -H 'Content-Type: application/json' \
   -d '{"Name": "ABC-456"}'

# Add items
curl -X PUT http://localhost:17322/inventories/4IVPNNHXSOZI/add-items \
   -H 'Content-Type: application/json' \
   -d '{"Count": 10}'

# Remove items
curl -X PUT http://localhost:17322/inventories/4IVPNNHXSOZI/remove-items \
   -H 'Content-Type: application/json' \
   -d '{"Count": 5}'

# Deactivate
curl -X PUT http://localhost:17322/inventories/4IVPNNHXSOZI/deactivate \
   -H 'Content-Type: application/json' \
   -d '{}'
```

A successful command call returns `202 Accepted` with a body such as:

```json
{
  "inventoryId": "4IVPNNHXSOZI",
  "commandId": "6605a0ed-edb1-4098-8f43-6de7361306d8",
  "correlationId": "40f8b0ac-bc8a-4980-a65f-c6092c7295cf",
  "causationId": "00000000-0000-0000-0000-000000000000"
}
```

The query endpoint returns the inventory view model:

```json
{
  "inventoryId": "4IVPNNHXSOZI",
  "name": "ABC-123",
  "stockQuantity": 0,
  "isActive": true
}
```
