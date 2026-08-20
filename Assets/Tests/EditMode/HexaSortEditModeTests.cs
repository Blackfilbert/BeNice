using System.Collections;
using System.Collections.Generic;
using BeNice.HexaSort;
using BeNice.HexaSort.Configs;
using BeNice.HexaSort.Models;
using BeNice.HexaSort.Services;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class HexaSortEditModeTests
{
    [Test]
    public void StackReportsTopColor()
    {
        var stack = Stack(HexTileColor.Red, HexTileColor.Blue);

        Assert.AreEqual(HexTileColor.Blue, stack.TopColor);
    }

    [Test]
    public void StackCountsTopContinuousGroup()
    {
        var stack = Stack(HexTileColor.Red, HexTileColor.Blue, HexTileColor.Blue);

        Assert.AreEqual(2, stack.CountTopGroup());
    }

    [Test]
    public void StackExtractsTopGroup()
    {
        var stack = Stack(HexTileColor.Red, HexTileColor.Green, HexTileColor.Green);

        var group = stack.ExtractTopGroup();

        CollectionAssert.AreEqual(new[] { HexTileColor.Green, HexTileColor.Green }, group);
        Assert.AreEqual(1, stack.Count);
    }

    [Test]
    public void StackAddsGroupOnTop()
    {
        var stack = Stack(HexTileColor.Red);

        stack.AddGroupOnTop(new[] { HexTileColor.Blue, HexTileColor.Blue });

        Assert.AreEqual(HexTileColor.Blue, stack.TopColor);
        Assert.AreEqual(3, stack.Count);
    }

    [Test]
    public void FactoryExpandsEachColorSelectionToThirdStack()
    {
        var config = ScriptableObject.CreateInstance<HexGameplayConfig>();
        var factory = new HexStackFactory(config, null);

        var stack = factory.CreateModel(new[] { HexTileColor.Red, HexTileColor.Blue, HexTileColor.Green });

        Assert.AreEqual(9, stack.Count);
        CollectionAssert.AreEqual(
            new[]
            {
                HexTileColor.Red, HexTileColor.Red, HexTileColor.Red,
                HexTileColor.Blue, HexTileColor.Blue, HexTileColor.Blue,
                HexTileColor.Green, HexTileColor.Green, HexTileColor.Green
            },
            stack.SnapshotBottomToTop());
    }

    [Test]
    public void CoordinatesReturnFixedNeighbors()
    {
        var origin = new HexCoordinates(0, 0);

        Assert.AreEqual(new HexCoordinates(1, 0), origin.GetNeighbor(0));
        Assert.AreEqual(new HexCoordinates(1, -1), origin.GetNeighbor(1));
        Assert.AreEqual(new HexCoordinates(0, -1), origin.GetNeighbor(2));
        Assert.AreEqual(new HexCoordinates(-1, 0), origin.GetNeighbor(3));
        Assert.AreEqual(new HexCoordinates(-1, 1), origin.GetNeighbor(4));
        Assert.AreEqual(new HexCoordinates(0, 1), origin.GetNeighbor(5));
    }

    [Test]
    public void BoardPlacesStackOnFreeCell()
    {
        var board = Board(new HexCoordinates(0, 0));

        Assert.IsTrue(board.TryPlaceStack(new HexCoordinates(0, 0), Stack(HexTileColor.Red)));
        Assert.IsTrue(board.GetCell(new HexCoordinates(0, 0)).HasStack);
    }

    [Test]
    public void BoardRejectsPlacementOnOccupiedCell()
    {
        var board = Board(new HexCoordinates(0, 0));
        board.TryPlaceStack(new HexCoordinates(0, 0), Stack(HexTileColor.Red));

        Assert.IsFalse(board.TryPlaceStack(new HexCoordinates(0, 0), Stack(HexTileColor.Blue)));
    }

    [Test]
    public void ReactionMergesOnlySameTopColor()
    {
        var board = Board(new HexCoordinates(0, 0), new HexCoordinates(1, 0));
        board.TryPlaceStack(new HexCoordinates(0, 0), Stack(HexTileColor.Red));
        board.TryPlaceStack(new HexCoordinates(1, 0), Stack(HexTileColor.Blue));
        var service = Reaction(board);

        Assert.IsFalse(service.TryBuildNextOperation(new HexCoordinates(0, 0), out _));
    }

    [Test]
    public void ReactionTransfersOnlyTopContinuousGroup()
    {
        var board = Board(new HexCoordinates(0, 0), new HexCoordinates(1, 0));
        board.TryPlaceStack(new HexCoordinates(0, 0), Stack(HexTileColor.Blue, HexTileColor.Red, HexTileColor.Red));
        board.TryPlaceStack(new HexCoordinates(1, 0), Stack(HexTileColor.Red));
        var service = Reaction(board);

        Assert.IsTrue(service.TryBuildNextOperation(new HexCoordinates(0, 0), out var operation));
        service.ApplyOperation(operation);

        Assert.AreEqual(1, board.GetCell(new HexCoordinates(0, 0)).Stack.Count);
        Assert.AreEqual(HexTileColor.Blue, board.GetCell(new HexCoordinates(0, 0)).Stack.TopColor);
        Assert.AreEqual(3, board.GetCell(new HexCoordinates(1, 0)).Stack.Count);
    }

    [Test]
    public void ReactionTransfersIntoStackWithFewerTiles()
    {
        var board = Board(new HexCoordinates(0, 0), new HexCoordinates(1, 0));
        board.TryPlaceStack(new HexCoordinates(0, 0), Stack(HexTileColor.Red));
        board.TryPlaceStack(new HexCoordinates(1, 0), Stack(HexTileColor.Blue, HexTileColor.Red, HexTileColor.Red));
        var service = Reaction(board);

        Assert.IsTrue(service.TryBuildNextOperation(new HexCoordinates(0, 0), out var operation));

        Assert.AreEqual(new HexCoordinates(1, 0), operation.Source);
        Assert.AreEqual(new HexCoordinates(0, 0), operation.Target);
        Assert.AreEqual(2, operation.Count);
    }

    [Test]
    public void ReactionClearsAtTenTiles()
    {
        var board = Board(new HexCoordinates(0, 0));
        board.TryPlaceStack(new HexCoordinates(0, 0), Stack(Repeat(HexTileColor.Red, 10)));
        var service = Reaction(board);

        Assert.IsTrue(service.TryBuildNextOperation(new HexCoordinates(0, 0), out var operation));
        Assert.AreEqual(HexReactionOperationType.Clear, operation.Type);
    }

    [Test]
    public void ReactionDoesNotClearAtNineTiles()
    {
        var board = Board(new HexCoordinates(0, 0));
        board.TryPlaceStack(new HexCoordinates(0, 0), Stack(Repeat(HexTileColor.Red, 9)));
        var service = Reaction(board);

        Assert.IsFalse(service.TryBuildNextOperation(new HexCoordinates(0, 0), out _));
    }

    [Test]
    public void ReactionContinuesAfterClearWithNewTopColor()
    {
        var board = Board(new HexCoordinates(0, 0), new HexCoordinates(1, 0));
        var colors = new List<HexTileColor> { HexTileColor.Blue };
        colors.AddRange(Repeat(HexTileColor.Red, 10));
        board.TryPlaceStack(new HexCoordinates(0, 0), Stack(colors));
        board.TryPlaceStack(new HexCoordinates(1, 0), Stack(HexTileColor.Blue));
        var service = Reaction(board);

        service.TryBuildNextOperation(new HexCoordinates(0, 0), out var clear);
        service.ApplyOperation(clear);

        Assert.IsTrue(service.TryBuildNextOperation(new HexCoordinates(0, 0), out var merge));
        Assert.AreEqual(HexReactionOperationType.Merge, merge.Type);
        Assert.AreEqual(new HexCoordinates(1, 0), merge.Source);
        Assert.AreEqual(new HexCoordinates(0, 0), merge.Target);
    }

    [Test]
    public void ReactionContinuesFromSourceAfterMergeExposesNewTopColor()
    {
        var active = new HexCoordinates(0, 0);
        var redNeighbor = new HexCoordinates(1, 0);
        var blueNeighbor = new HexCoordinates(0, 1);
        var board = Board(active, redNeighbor, blueNeighbor);
        board.TryPlaceStack(active, Stack(HexTileColor.Blue, HexTileColor.Red, HexTileColor.Red));
        board.TryPlaceStack(redNeighbor, Stack(HexTileColor.Red));
        board.TryPlaceStack(blueNeighbor, Stack(HexTileColor.Blue));
        var service = Reaction(board);

        var routine = service.Resolve(active, new ImmediateAnimator(), default, null, null);
        while (routine.MoveNext())
        {
            if (routine.Current is IEnumerator nested)
            {
                while (nested.MoveNext())
                {
                }
            }
        }

        Assert.AreEqual(2, board.GetCell(active).Stack.Count);
        Assert.AreEqual(HexTileColor.Blue, board.GetCell(active).Stack.TopColor);
        Assert.IsFalse(board.GetCell(blueNeighbor).HasStack);
    }

    [Test]
    public void MixedMatchingPairClearsInOnePlacement()
    {
        var active = new HexCoordinates(0, 0);
        var neighbor = new HexCoordinates(1, 0);
        var board = Board(active, neighbor);
        board.TryPlaceStack(
            active,
            StackFromSelections(HexTileColor.Red, HexTileColor.Red, HexTileColor.Purple));
        board.TryPlaceStack(
            neighbor,
            StackFromSelections(HexTileColor.Red, HexTileColor.Purple, HexTileColor.Purple));
        var service = Reaction(board);

        var routine = service.Resolve(active, new ImmediateAnimator(), default, null, null);
        while (routine.MoveNext())
        {
            if (routine.Current is IEnumerator nested)
            {
                while (nested.MoveNext())
                {
                }
            }
        }

        Assert.IsFalse(board.HasStacks);
    }

    [Test]
    public void BoardRemovesEmptyStack()
    {
        var board = Board(new HexCoordinates(0, 0));
        board.TryPlaceStack(new HexCoordinates(0, 0), Stack(HexTileColor.Red));
        board.GetCell(new HexCoordinates(0, 0)).Stack.ExtractTopTiles(1);

        board.RemoveEmptyStack(new HexCoordinates(0, 0));

        Assert.IsFalse(board.GetCell(new HexCoordinates(0, 0)).HasStack);
    }

    [Test]
    public void ReactionUsesDeterministicNeighborOrder()
    {
        var board = Board(new HexCoordinates(0, 0), new HexCoordinates(1, 0), new HexCoordinates(0, 1));
        board.TryPlaceStack(new HexCoordinates(0, 0), Stack(HexTileColor.Red));
        board.TryPlaceStack(new HexCoordinates(1, 0), Stack(HexTileColor.Red));
        board.TryPlaceStack(new HexCoordinates(0, 1), Stack(HexTileColor.Red));
        var service = Reaction(board);

        service.TryBuildNextOperation(new HexCoordinates(0, 0), out var operation);

        Assert.AreEqual(new HexCoordinates(1, 0), operation.Source);
        Assert.AreEqual(new HexCoordinates(0, 0), operation.Target);
    }

    [Test]
    public void ReactionStopsAtStepLimit()
    {
        var board = Board(new HexCoordinates(0, 0), new HexCoordinates(1, 0));
        board.TryPlaceStack(new HexCoordinates(0, 0), Stack(Repeat(HexTileColor.Red, 9)));
        board.TryPlaceStack(new HexCoordinates(1, 0), Stack(HexTileColor.Red));
        var service = Reaction(board, 1);
        var result = HexReactionResult.Completed;

        var routine = service.Resolve(
            new HexCoordinates(0, 0),
            new ImmediateAnimator(),
            default,
            null,
            value => result = value);
        while (routine.MoveNext())
        {
            if (routine.Current is IEnumerator nested)
            {
                while (nested.MoveNext())
                {
                }
            }
        }

        Assert.AreEqual(HexReactionResult.StepLimitExceeded, result);
        Assert.IsFalse(service.IsRunning);
    }

    private static HexBoardModel Board(params HexCoordinates[] coordinates)
    {
        var board = new HexBoardModel();
        board.Initialize(coordinates);
        return board;
    }

    private static HexReactionService Reaction(HexBoardModel board, int? maxSteps = null)
    {
        var config = ScriptableObject.CreateInstance<HexGameplayConfig>();
        if (maxSteps.HasValue)
        {
            var serialized = new SerializedObject(config);
            serialized.FindProperty("_maxReactionSteps").intValue = maxSteps.Value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        return new HexReactionService(board, config);
    }

    private static HexStackModel Stack(params HexTileColor[] colors) => new HexStackModel(colors);

    private static HexStackModel Stack(IReadOnlyList<HexTileColor> colors) => new HexStackModel(colors);

    private static HexStackModel StackFromSelections(params HexTileColor[] selections)
    {
        var colors = new List<HexTileColor>(selections.Length * 3);
        for (var selectionIndex = 0; selectionIndex < selections.Length; selectionIndex++)
        {
            for (var tileIndex = 0; tileIndex < 3; tileIndex++)
                colors.Add(selections[selectionIndex]);
        }

        return Stack(colors);
    }

    private static HexTileColor[] Repeat(HexTileColor color, int count)
    {
        var result = new HexTileColor[count];
        for (var i = 0; i < count; i++)
            result[i] = color;
        return result;
    }

    private sealed class ImmediateAnimator : IHexReactionAnimator
    {
        public IEnumerator PlayOperation(HexReactionOperation operation, float speedMultiplier)
        {
            yield break;
        }
    }
}
