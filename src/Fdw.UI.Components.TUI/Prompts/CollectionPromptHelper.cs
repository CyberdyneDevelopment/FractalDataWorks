using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Conventions;
using Spectre.Console;

#pragma warning disable MA0016 // Prefer using collection abstraction instead of implementation - TUI requires concrete List<T> for modification

namespace Fdw.UI.Components.TUI.Prompts;

/// <summary>
/// Helper for interactive collection management.
/// </summary>
public static class CollectionPromptHelper
{
    /// <summary>
    /// Prompts the user to manage a collection interactively.
    /// </summary>
    /// <typeparam name="T">The item type</typeparam>
    /// <typeparam name="TComponent">The component type for items</typeparam>
    /// <param name="console">The console to prompt on</param>
    /// <param name="items">The current collection of items</param>
    /// <param name="createComponent">Function to create a component for an item</param>
    /// <param name="createNewItem">Function to create a new item</param>
    /// <param name="theme">Theme configuration</param>
    /// <returns>The updated collection</returns>
    public static async Task<List<T>> PromptCollection<T, TComponent>(
        IAnsiConsole console,
        List<T>? items,
        Func<T, int, TComponent> createComponent,
        Func<T> createNewItem,
        TUIThemeConfiguration? theme = null)
        where TComponent : TUIComponent<TComponent, T>
    {
        items ??= [];

        while (true)
        {
            console.Clear();
            RenderCollectionTable(console, items, createComponent, theme);

            List<string> choices = [];
            if (items.Count > 0)
            {
                choices.Add("View Item");
                choices.Add("Edit Item");
                choices.Add("Remove Item");
            }
            choices.Add("Add Item");
            choices.Add("Done");

            var action = console.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Collection Actions:[/]")
                    .AddChoices(choices));

            switch (action)
            {
                case "View Item":
                    await ViewItem(console, items, createComponent).ConfigureAwait(false);
                    break;
                case "Edit Item":
                    await EditItem(console, items, createComponent).ConfigureAwait(false);
                    break;
                case "Remove Item":
                    RemoveItem(console, items);
                    break;
                case "Add Item":
                    await AddItem(console, items, createNewItem, createComponent).ConfigureAwait(false);
                    break;
                case "Done":
                    return items;
            }

            console.WriteLine();
            console.Write("Press any key to continue...");
            Console.ReadKey(true);
        }
    }

    [ConventionOverride(MaxCyclomaticComplexity = 15)]  // UI rendering logic — theme fallbacks and table styling
    private static void RenderCollectionTable<T, TComponent>(
        IAnsiConsole console,
        List<T> items,
        Func<T, int, TComponent> createComponent,
        TUIThemeConfiguration? theme)
        where TComponent : TUIComponent<TComponent, T>
    {
        if (items.Count == 0)
        {
            console.MarkupLine($"[{theme?.Colors.Info ?? Color.Grey}]No items in collection[/]");
            return;
        }

        var border = theme?.Borders.Table ?? TableBorder.Rounded;
        var table = new Table()
            .Border(border)
            .BorderColor(theme?.Colors.Primary ?? Color.Blue)
            .AddColumn("[bold]#[/]")
            .AddColumn("[bold]Item[/]");

        for (int i = 0; i < items.Count; i++)
        {
            var component = createComponent(items[i], i);
            table.AddRow(
                $"[{theme?.Colors.Primary ?? Color.Blue}]{i + 1}[/]",
                component.GetDisplayText());
        }

        console.Write(table);
    }

    private static async Task ViewItem<T, TComponent>(
        IAnsiConsole console,
        List<T> items,
        Func<T, int, TComponent> createComponent)
        where TComponent : TUIComponent<TComponent, T>
    {
        if (items.Count == 0) return;

        var index = SelectItemIndex(console, items.Count, "View which item?");
        if (index < 0) return;

        console.Clear();
        var component = createComponent(items[index], index);
        component.Render(console);
    }

    private static async Task EditItem<T, TComponent>(
        IAnsiConsole console,
        List<T> items,
        Func<T, int, TComponent> createComponent)
        where TComponent : TUIComponent<TComponent, T>
    {
        if (items.Count == 0) return;

        var index = SelectItemIndex(console, items.Count, "Edit which item?");
        if (index < 0) return;

        console.Clear();
        var component = createComponent(items[index], index);
        var updated = await component.Prompt(console).ConfigureAwait(false);

        if (updated != null)
        {
            items[index] = updated;
            console.MarkupLine("[green]Item updated successfully![/]");
        }
    }

    private static void RemoveItem<T>(IAnsiConsole console, List<T> items)
    {
        if (items.Count == 0) return;

        var index = SelectItemIndex(console, items.Count, "Remove which item?");
        if (index < 0) return;

        if (console.Confirm($"Remove item {index + 1}?", false))
        {
            items.RemoveAt(index);
            console.MarkupLine("[green]Item removed successfully![/]");
        }
    }

    private static async Task AddItem<T, TComponent>(
        IAnsiConsole console,
        List<T> items,
        Func<T> createNewItem,
        Func<T, int, TComponent> createComponent)
        where TComponent : TUIComponent<TComponent, T>
    {
        console.Clear();
        var newItem = createNewItem();
        var component = createComponent(newItem, items.Count);
        var result = await component.Prompt(console).ConfigureAwait(false);

        if (result != null)
        {
            items.Add(result);
            console.MarkupLine("[green]Item added successfully![/]");
        }
    }

    private static int SelectItemIndex(IAnsiConsole console, int count, string title)
    {
        var choices = Enumerable.Range(1, count)
            .Select(i => i.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .ToList();

        var selection = console.Prompt(
            new SelectionPrompt<string>()
                .Title(title)
                .AddChoices(choices));

        return int.Parse(selection, System.Globalization.CultureInfo.InvariantCulture) - 1;
    }
}
