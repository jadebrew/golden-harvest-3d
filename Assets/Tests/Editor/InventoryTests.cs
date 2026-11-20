using System.Collections.Generic;
using Data;
using NUnit.Framework;
using UnityEngine;

public class InventoryTests
{
    private Item _wheat;
    private Item _bread;

    [SetUp]
    public void SetUp()
    {
        _wheat = ScriptableObject.CreateInstance<Item>();
        _bread = ScriptableObject.CreateInstance<Item>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_wheat);
        Object.DestroyImmediate(_bread);
    }

    [Test]
    public void Add_AccumulatesAmounts()
    {
        var inventory = new Inventory();
        inventory.Add(_wheat, 2);
        inventory.Add(_wheat, 3);
        Assert.AreEqual(5, inventory.Count(_wheat));
    }

    [Test]
    public void TryRemove_FailsWithoutEnough_AndClearsEmptyEntries()
    {
        var inventory = new Inventory();
        inventory.Add(_wheat, 2);

        Assert.IsFalse(inventory.TryRemove(_wheat, 3));
        Assert.AreEqual(2, inventory.Count(_wheat));

        Assert.IsTrue(inventory.TryRemove(_wheat, 2));
        Assert.IsTrue(inventory.IsEmpty);
    }

    [Test]
    public void TryMoveTo_MovesAllOrNothing()
    {
        var home = new Inventory();
        var hands = new Inventory();
        home.Add(_wheat, 3);
        var recipe = new Dictionary<Item, int> { { _wheat, 2 }, { _bread, 1 } };

        Assert.IsFalse(home.TryMoveTo(hands, recipe));
        Assert.AreEqual(3, home.Count(_wheat));
        Assert.IsTrue(hands.IsEmpty);

        home.Add(_bread, 1);
        Assert.IsTrue(home.TryMoveTo(hands, recipe));
        Assert.AreEqual(1, home.Count(_wheat));
        Assert.AreEqual(2, hands.Count(_wheat));
        Assert.AreEqual(1, hands.Count(_bread));
    }

    [Test]
    public void MoveAllTo_RespectsFilter()
    {
        var home = new Inventory();
        var cart = new Inventory();
        home.Add(_wheat, 4);
        home.Add(_bread, 2);

        home.MoveAllTo(cart, item => item == _bread);

        Assert.AreEqual(4, home.Count(_wheat));
        Assert.AreEqual(0, home.Count(_bread));
        Assert.AreEqual(2, cart.Count(_bread));
    }
}
