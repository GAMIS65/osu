// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Testing;
using osu.Game.Online.Rooms;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.OnlinePlay.Lounge;
using osu.Game.Screens.OnlinePlay.Lounge.Components;
using osu.Game.Tests.Visual.OnlinePlay;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Multiplayer
{
    public partial class TestSceneLoungeRoomsContainer : OnlinePlayTestScene
    {
        protected new TestRoomManager RoomManager => (TestRoomManager)base.RoomManager;

        private RoomsContainer container = null!;

        public override void SetUpSteps()
        {
            base.SetUpSteps();

            AddStep("create container", () =>
            {
                Child = new PopoverContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Width = 0.5f,

                    Child = container = new RoomsContainer
                    {
                        SelectedRoom = { BindTarget = SelectedRoom }
                    }
                };
            });
        }

        [Test]
        public void TestBasicListChanges()
        {
            AddStep("add rooms", () => RoomManager.AddRooms(5, withSpotlightRooms: true));

            AddAssert("has 5 rooms", () => container.DrawableRooms.Count == 5);

            AddAssert("all spotlights at top", () => container.DrawableRooms
                                                              .SkipWhile(r => r.Room.Category == RoomCategory.Spotlight)
                                                              .All(r => r.Room.Category == RoomCategory.Normal));

            AddStep("remove first room", () => RoomManager.RemoveRoom(RoomManager.Rooms.First(r => r.RoomID == 0)));
            AddAssert("has 4 rooms", () => container.DrawableRooms.Count == 4);
            AddAssert("first room removed", () => container.DrawableRooms.All(r => r.Room.RoomID != 0));

            AddStep("select first room", () => container.DrawableRooms.First().TriggerClick());
            AddAssert("first spotlight selected", () => checkRoomSelected(RoomManager.Rooms.First(r => r.Category == RoomCategory.Spotlight)));

            AddStep("remove last room", () => RoomManager.RemoveRoom(RoomManager.Rooms.MinBy(r => r.RoomID)!));
            AddAssert("first spotlight still selected", () => checkRoomSelected(RoomManager.Rooms.First(r => r.Category == RoomCategory.Spotlight)));

            AddStep("remove spotlight room", () => RoomManager.RemoveRoom(RoomManager.Rooms.Single(r => r.Category == RoomCategory.Spotlight)));
            AddAssert("selection vacated", () => checkRoomSelected(null));
        }

        [Test]
        public void TestKeyboardNavigation()
        {
            AddStep("add rooms", () => RoomManager.AddRooms(3));

            AddAssert("no selection", () => checkRoomSelected(null));

            press(Key.Down);
            AddAssert("first room selected", () => checkRoomSelected(RoomManager.Rooms.First()));

            press(Key.Up);
            AddAssert("first room selected", () => checkRoomSelected(RoomManager.Rooms.First()));

            press(Key.Down);
            press(Key.Down);
            AddAssert("last room selected", () => checkRoomSelected(RoomManager.Rooms.Last()));
        }

        [Test]
        public void TestKeyboardNavigationAfterOrderChange()
        {
            AddStep("add rooms", () => RoomManager.AddRooms(3));

            AddStep("reorder rooms", () =>
            {
                var room = RoomManager.Rooms[1];

                RoomManager.RemoveRoom(room);
                RoomManager.AddOrUpdateRoom(room);
            });

            AddAssert("no selection", () => checkRoomSelected(null));

            press(Key.Down);
            AddAssert("first room selected", () => checkRoomSelected(getRoomInFlow(0)));

            press(Key.Down);
            AddAssert("second room selected", () => checkRoomSelected(getRoomInFlow(1)));

            press(Key.Down);
            AddAssert("third room selected", () => checkRoomSelected(getRoomInFlow(2)));
        }

        [Test]
        public void TestClickDeselection()
        {
            AddStep("add room", () => RoomManager.AddRooms(1));

            AddAssert("no selection", () => checkRoomSelected(null));

            press(Key.Down);
            AddAssert("first room selected", () => checkRoomSelected(RoomManager.Rooms.First()));

            AddStep("click away", () => InputManager.Click(MouseButton.Left));
            AddAssert("no selection", () => checkRoomSelected(null));
        }

        private void press(Key down)
        {
            AddStep($"press {down}", () => InputManager.Key(down));
        }

        [Test]
        public void TestStringFiltering()
        {
            AddStep("add rooms", () => RoomManager.AddRooms(4));

            AddUntilStep("4 rooms visible", () => container.DrawableRooms.Count(r => r.IsPresent) == 4);

            AddStep("filter one room", () => container.Filter.Value = new FilterCriteria { SearchString = "1" });

            AddUntilStep("1 rooms visible", () => container.DrawableRooms.Count(r => r.IsPresent) == 1);

            AddStep("remove filter", () => container.Filter.Value = null);

            AddUntilStep("4 rooms visible", () => container.DrawableRooms.Count(r => r.IsPresent) == 4);
        }

        [Test]
        public void TestRulesetFiltering()
        {
            AddStep("add rooms", () => RoomManager.AddRooms(2, new OsuRuleset().RulesetInfo));
            AddStep("add rooms", () => RoomManager.AddRooms(3, new CatchRuleset().RulesetInfo));

            // Todo: What even is this case...?
            AddStep("set empty filter criteria", () => container.Filter.Value = new FilterCriteria());
            AddUntilStep("5 rooms visible", () => container.DrawableRooms.Count(r => r.IsPresent) == 5);

            AddStep("filter osu! rooms", () => container.Filter.Value = new FilterCriteria { Ruleset = new OsuRuleset().RulesetInfo });
            AddUntilStep("2 rooms visible", () => container.DrawableRooms.Count(r => r.IsPresent) == 2);

            AddStep("filter catch rooms", () => container.Filter.Value = new FilterCriteria { Ruleset = new CatchRuleset().RulesetInfo });
            AddUntilStep("3 rooms visible", () => container.DrawableRooms.Count(r => r.IsPresent) == 3);
        }

        [Test]
        public void TestAccessTypeFiltering()
        {
            AddStep("add rooms", () =>
            {
                RoomManager.AddRooms(1, withPassword: true);
                RoomManager.AddRooms(1, withPassword: false);
            });

            AddStep("apply default filter", () => container.Filter.SetDefault());

            AddUntilStep("both rooms visible", () => container.DrawableRooms.Count(r => r.IsPresent) == 2);

            AddStep("filter public rooms", () => container.Filter.Value = new FilterCriteria { Permissions = RoomPermissionsFilter.Public });

            AddUntilStep("private room hidden", () => container.DrawableRooms.All(r => !r.Room.HasPassword));

            AddStep("filter private rooms", () => container.Filter.Value = new FilterCriteria { Permissions = RoomPermissionsFilter.Private });

            AddUntilStep("public room hidden", () => container.DrawableRooms.All(r => r.Room.HasPassword));
        }

        [Test]
        public void TestPasswordProtectedRooms()
        {
            AddStep("add rooms", () => RoomManager.AddRooms(3, withPassword: true));
        }

        private bool checkRoomSelected(Room? room) => SelectedRoom.Value == room;

        private Room? getRoomInFlow(int index) =>
            (container.ChildrenOfType<FillFlowContainer<DrawableLoungeRoom>>().First().FlowingChildren.ElementAt(index) as DrawableRoom)?.Room;
    }
}
