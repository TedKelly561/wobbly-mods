using System.Collections.Generic;
using WobblyMods.Core;
using Xunit;

public class PropRoomsTests
{
    [Fact]
    public void ClassifyNullReturnsOther()
    {
        Assert.Equal(Room.Other, PropRooms.Classify(null));
    }

    [Fact]
    public void ClassifyEmptyReturnsOther()
    {
        Assert.Equal(Room.Other, PropRooms.Classify(""));
    }

    // Kids Tests
    [Fact]
    public void ClassifyKidsToyBox()
    {
        Assert.Equal(Room.Kids, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Props/Kids/ToyBox_01.prefab"));
    }

    [Fact]
    public void ClassifyKidsTeddyBear()
    {
        Assert.Equal(Room.Kids, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Props/TeddyBear_Blue_02.prefab"));
    }

    [Fact]
    public void ClassifyKidsDoll()
    {
        Assert.Equal(Room.Kids, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Props/Doll_01.prefab"));
    }

    [Fact]
    public void ClassifyKidsBunkBedWinsOverBedroom()
    {
        // "bunk" is Kids keyword. "bed" is Bedroom keyword. 
        // Order: Kids, Bathroom, Kitchen, Bedroom... so Kids wins if "bunk" is present.
        Assert.Equal(Room.Kids, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Props/BunkBed_Blue_02.prefab"));
    }

    [Fact]
    public void ClassifyKidsFolderWins()
    {
        Assert.Equal(Room.Kids, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Toys/Doll_01.prefab"));
    }

    // Bathroom Tests
    [Fact]
    public void ClassifyBathroomSink()
    {
        Assert.Equal(Room.Bathroom, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Props/BathroomSink_01.prefab"));
    }

    [Fact]
    public void ClassifyBathroomToilet()
    {
        Assert.Equal(Room.Bathroom, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Props/Toilet_White_02.prefab"));
    }

    [Fact]
    public void ClassifyBathroomBathtub()
    {
        Assert.Equal(Room.Bathroom, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Props/Bathtub_01.prefab"));
    }

    [Fact]
    public void ClassifyBathroomFolderWins()
    {
        Assert.Equal(Room.Bathroom, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Bathroom/Mirror_01.prefab"));
    }

    // Kitchen Tests
    [Fact]
    public void ClassifyKitchenFridge()
    {
        Assert.Equal(Room.Kitchen, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Props/Kitchen/Fridge_01.prefab"));
    }

    [Fact]
    public void ClassifyKitchenOven()
    {
        Assert.Equal(Room.Kitchen, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Props/Oven_01.prefab"));
    }

    [Fact]
    public void ClassifyKitchenMicrowave()
    {
        Assert.Equal(Room.Kitchen, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Props/Microwave_02.prefab"));
    }

    [Fact]
    public void ClassifyKitchenFolderWins()
    {
        Assert.Equal(Room.Kitchen, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Kitchen/Cupboard_01.prefab"));
    }

    // Bedroom Tests
    [Fact]
    public void ClassifyBedroomBed()
    {
        Assert.Equal(Room.Bedroom, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Props/Bed_King_01.prefab"));
    }

    [Fact]
    public void ClassifyBedroomWardrobe()
    {
        Assert.Equal(Room.Bedroom, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Props/Wardrobe_Wood_02.prefab"));
    }

    [Fact]
    public void ClassifyBedroomNightstand()
    {
        Assert.Equal(Room.Bedroom, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Props/Nightstand_01.prefab"));
    }

    [Fact]
    public void ClassifyBedroomFolderWins()
    {
        Assert.Equal(Room.Bedroom, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Bedroom/Dresser_01.prefab"));
    }

    // Living Tests
    [Fact]
    public void ClassifyLivingSofa()
    {
        Assert.Equal(Room.Living, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Props/Sofa_Gray_01.prefab"));
    }

    [Fact]
    public void ClassifyLivingTable()
    {
        Assert.Equal(Room.Living, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Props/Table_Wood_02.prefab"));
    }

    [Fact]
    public void ClassifyLivingLamp()
    {
        Assert.Equal(Room.Living, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Props/Lamp_Floor_01.prefab"));
    }

    [Fact]
    public void ClassifyLivingTV()
    {
        Assert.Equal(Room.Living, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Props/TV_Large_01.prefab"));
    }

    // Garden Tests
    [Fact]
    public void ClassifyGardenTree()
    {
        Assert.Equal(Room.Garden, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Props/Tree_Oak_01.prefab"));
    }

    [Fact]
    public void ClassifyGardenFlower()
    {
        Assert.Equal(Room.Garden, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Props/Flower_Bouquet_02.prefab"));
    }

    [Fact]
    public void ClassifyGardenBBQ()
    {
        Assert.Equal(Room.Garden, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Props/BBQ_Grill_01.prefab"));
    }

    [Fact]
    public void ClassifyGardenFolderWins()
    {
        Assert.Equal(Room.Garden, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Garden/Bench_01.prefab"));
    }

    [Fact]
    public void ClassifyOutdoorFolderWins()
    {
        Assert.Equal(Room.Garden, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Outdoor/Fence_01.prefab"));
    }

    // Other Tests
    [Fact]
    public void ClassifyOtherUnknownItem()
    {
        Assert.Equal(Room.Other, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Props/RandomBox_01.prefab"));
    }

    [Fact]
    public void ClassifyOtherNoKeywords()
    {
        Assert.Equal(Room.Other, PropRooms.Classify("Assets/Content/Arcade/Sandbox/Prefabs/Props/Thingy_01.prefab"));
    }

    // NiceName Tests
    [Fact]
    public void NiceNameNullReturnsThing()
    {
        Assert.Equal("thing", PropRooms.NiceName(null));
    }

    [Fact]
    public void NiceNameEmptyReturnsThing()
    {
        Assert.Equal("thing", PropRooms.NiceName(""));
    }

    [Fact]
    public void NiceNameSimpleWord()
    {
        Assert.Equal("fridge", PropRooms.NiceName("Assets/Content/Arcade/Sandbox/Prefabs/Props/Kitchen/Fridge_01.prefab"));
    }

    [Fact]
    public void NiceNameCamelCase()
    {
        Assert.Equal("bunk bed blue", PropRooms.NiceName("Assets/Content/Arcade/Sandbox/Prefabs/Props/BunkBed_Blue_02.prefab"));
    }

    [Fact]
    public void NiceNameDropsNumbersAndPrefab()
    {
        Assert.Equal("fridge", PropRooms.NiceName("Assets/Content/Arcade/Sandbox/Prefabs/Props/Kitchen/Fridge_01.prefab"));
    }

    [Fact]
    public void NiceNameDropsLod()
    {
        Assert.Equal("fridge", PropRooms.NiceName("Assets/Content/Arcade/Sandbox/Prefabs/Props/Kitchen/Fridge_lod0.prefab"));
    }

    [Fact]
    public void NiceNameEmptyResultReturnsThing()
    {
        Assert.Equal("thing", PropRooms.NiceName("Assets/Content/Arcade/Sandbox/Prefabs/Props/123_456.prefab"));
    }

    // LooksLikeFurniture Tests
    [Fact]
    public void LooksLikeFurnitureNullReturnsFalse()
    {
        Assert.False(PropRooms.LooksLikeFurniture(null));
    }

    [Fact]
    public void LooksLikeFurnitureEmptyReturnsFalse()
    {
        Assert.False(PropRooms.LooksLikeFurniture(""));
    }

    [Fact]
    public void LooksLikeFurnitureNoPropsFolderReturnsFalse()
    {
        Assert.False(PropRooms.LooksLikeFurniture("Assets/Content/Arcade/Sandbox/Prefabs/Items/Fridge_01.prefab"));
    }

    [Fact]
    public void LooksLikeFurnitureValidPropsPathReturnsTrue()
    {
        Assert.True(PropRooms.LooksLikeFurniture("Assets/Content/Arcade/Sandbox/Prefabs/Props/Kitchen/Fridge_01.prefab"));
    }

    [Fact]
    public void LooksLikeFurnitureValidFurnitureFolderReturnsTrue()
    {
        Assert.True(PropRooms.LooksLikeFurniture("Assets/Content/Arcade/Sandbox/Prefabs/Furniture/Table_01.prefab"));
    }

    [Fact]
    public void LooksLikeFurnitureExclusionVehicleReturnsFalse()
    {
        Assert.False(PropRooms.LooksLikeFurniture("Assets/Content/Arcade/Sandbox/Prefabs/Props/Vehicle_Car_01.prefab"));
    }

    [Fact]
    public void LooksLikeFurnitureExclusionNPCReturnsFalse()
    {
        Assert.False(PropRooms.LooksLikeFurniture("Assets/Content/Arcade/Sandbox/Prefabs/Props/NPC_Human_01.prefab"));
    }

    [Fact]
    public void LooksLikeFurnitureExclusionPlayerReturnsFalse()
    {
        Assert.False(PropRooms.LooksLikeFurniture("Assets/Content/Arcade/Sandbox/Prefabs/Props/Player_Character_01.prefab"));
    }

    [Fact]
    public void LooksLikeFurnitureExclusionCharacterReturnsFalse()
    {
        Assert.False(PropRooms.LooksLikeFurniture("Assets/Content/Arcade/Sandbox/Prefabs/Props/Character_Hero_01.prefab"));
    }

    [Fact]
    public void LooksLikeFurnitureExclusionFXReturnsFalse()
    {
        Assert.False(PropRooms.LooksLikeFurniture("Assets/Content/Arcade/Sandbox/Prefabs/Props/FX_Spark_01.prefab"));
    }

    [Fact]
    public void LooksLikeFurnitureExclusionVFXReturnsFalse()
    {
        Assert.False(PropRooms.LooksLikeFurniture("Assets/Content/Arcade/Sandbox/Prefabs/Props/VFX_Fire_01.prefab"));
    }

    [Fact]
    public void LooksLikeFurnitureExclusionParticleReturnsFalse()
    {
        Assert.False(PropRooms.LooksLikeFurniture("Assets/Content/Arcade/Sandbox/Prefabs/Props/Particle_Dust_01.prefab"));
    }

    [Fact]
    public void LooksLikeFurnitureExclusionEffectReturnsFalse()
    {
        Assert.False(PropRooms.LooksLikeFurniture("Assets/Content/Arcade/Sandbox/Prefabs/Props/Effect_Blast_01.prefab"));
    }

    [Fact]
    public void LooksLikeFurnitureExclusionUIReturnsFalse()
    {
        Assert.False(PropRooms.LooksLikeFurniture("Assets/Content/Arcade/Sandbox/Prefabs/Props/UI_Button_01.prefab"));
    }

    [Fact]
    public void LooksLikeFurnitureExclusionColliderReturnsFalse()
    {
        Assert.False(PropRooms.LooksLikeFurniture("Assets/Content/Arcade/Sandbox/Prefabs/Props/Collider_Box_01.prefab"));
    }

    [Fact]
    public void LooksLikeFurnitureExclusionTriggerReturnsFalse()
    {
        Assert.False(PropRooms.LooksLikeFurniture("Assets/Content/Arcade/Sandbox/Prefabs/Props/Trigger_Area_01.prefab"));
    }

    [Fact]
    public void LooksLikeFurnitureExclusionSpawnerReturnsFalse()
    {
        Assert.False(PropRooms.LooksLikeFurniture("Assets/Content/Arcade/Sandbox/Prefabs/Props/Spawner_Enemy_01.prefab"));
    }

    [Fact]
    public void LooksLikeFurnitureExclusionTestReturnsFalse()
    {
        Assert.False(PropRooms.LooksLikeFurniture("Assets/Content/Arcade/Sandbox/Prefabs/Props/Test_Box_01.prefab"));
    }

    [Fact]
    public void LooksLikeFurnitureExclusionDebugReturnsFalse()
    {
        Assert.False(PropRooms.LooksLikeFurniture("Assets/Content/Arcade/Sandbox/Prefabs/Props/Debug_Visual_01.prefab"));
    }

    [Fact]
    public void LooksLikeFurnitureExclusionLodReturnsFalse()
    {
        Assert.False(PropRooms.LooksLikeFurniture("Assets/Content/Arcade/Sandbox/Prefabs/Props/Lod_Box_01.prefab"));
    }

    [Fact]
    public void LooksLikeFurnitureExclusionWallReturnsFalse()
    {
        Assert.False(PropRooms.LooksLikeFurniture("Assets/Content/Arcade/Sandbox/Prefabs/Props/Wall_Brick_01.prefab"));
    }

    [Fact]
    public void LooksLikeFurnitureExclusionFloorReturnsFalse()
    {
        Assert.False(PropRooms.LooksLikeFurniture("Assets/Content/Arcade/Sandbox/Prefabs/Props/Floor_Tile_01.prefab"));
    }

    [Fact]
    public void LooksLikeFurnitureExclusionCeilingReturnsFalse()
    {
        Assert.False(PropRooms.LooksLikeFurniture("Assets/Content/Arcade/Sandbox/Prefabs/Props/Ceiling_Plaster_01.prefab"));
    }

    [Fact]
    public void LooksLikeFurnitureExclusionDoorReturnsFalse()
    {
        Assert.False(PropRooms.LooksLikeFurniture("Assets/Content/Arcade/Sandbox/Prefabs/Props/Door_Wood_01.prefab"));
    }

    [Fact]
    public void LooksLikeFurnitureExclusionWindowReturnsFalse()
    {
        Assert.False(PropRooms.LooksLikeFurniture("Assets/Content/Arcade/Sandbox/Prefabs/Props/Window_Glass_01.prefab"));
    }

    [Fact]
    public void LooksLikeFurnitureExclusionRoadReturnsFalse()
    {
        Assert.False(PropRooms.LooksLikeFurniture("Assets/Content/Arcade/Sandbox/Prefabs/Props/Road_Asphalt_01.prefab"));
    }

    [Fact]
    public void LooksLikeFurnitureExclusionBuildingReturnsFalse()
    {
        Assert.False(PropRooms.LooksLikeFurniture("Assets/Content/Arcade/Sandbox/Prefabs/Props/Building_House_01.prefab"));
    }

    [Fact]
    public void LooksLikeFurnitureExclusionTerrainReturnsFalse()
    {
        Assert.False(PropRooms.LooksLikeFurniture("Assets/Content/Arcade/Sandbox/Prefabs/Props/Terrain_Grass_01.prefab"));
    }
}
