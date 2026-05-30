namespace CQRS.Benchmarks;

public sealed class InventoryScenario(InventoryBenchmarkClient client, BenchmarkSettings settings)
{
    public async Task<List<BenchmarkRecord>> RunAsync(int concurrencyLevel)
    {
        var records = new List<BenchmarkRecord>();

        var createResult = await client.CreateInventory($"Bench-{Guid.NewGuid():N}");
        records.Add(ToRecord(concurrencyLevel, "CreateInventory", createResult));

        if (createResult.Status != "Completed" || createResult.InventoryId is null)
            return records;

        var inventoryId = createResult.InventoryId;
        var stock = 0;

        for (var i = 0; i < settings.IterationsPerInventory; i++)
        {
            var addResult = await client.AddItems(inventoryId, settings.ItemsToAddPerIteration);
            records.Add(ToRecord(concurrencyLevel, "AddItemsToInventory", addResult));
            if (addResult.Status == "Completed")
                stock += settings.ItemsToAddPerIteration;

            if (settings.RenameEveryNIterations > 0 && i % settings.RenameEveryNIterations == 0)
            {
                var renameResult = await client.RenameInventory(
                    inventoryId,
                    $"Bench-{Guid.NewGuid():N}"
                );
                records.Add(ToRecord(concurrencyLevel, "RenameInventory", renameResult));
            }

            if (stock >= settings.ItemsToRemovePerIteration)
            {
                var removeResult = await client.RemoveItems(
                    inventoryId,
                    settings.ItemsToRemovePerIteration
                );
                records.Add(ToRecord(concurrencyLevel, "RemoveItemsFromInventory", removeResult));
                if (removeResult.Status == "Completed")
                    stock -= settings.ItemsToRemovePerIteration;
            }
        }

        if (stock > 0)
        {
            var clearResult = await client.RemoveItems(inventoryId, stock);
            records.Add(ToRecord(concurrencyLevel, "RemoveItemsFromInventory", clearResult));
        }

        var deactivateResult = await client.Deactivate(inventoryId);
        records.Add(ToRecord(concurrencyLevel, "DeactivateInventory", deactivateResult));

        return records;
    }

    private static BenchmarkRecord ToRecord(
        int concurrencyLevel,
        string commandType,
        CommandOutcome outcome
    ) => new(concurrencyLevel, commandType, outcome.Status, outcome.ElapsedMs);
}
