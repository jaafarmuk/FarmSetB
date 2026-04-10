using UnityEngine;

[System.Serializable]
public class FarmGridCellData
{
    public Vector2Int Coordinates;
    public FarmTileState State;
    public FarmCropType CropType;
    public int GrowthStage;

    public FarmGridCellData(int x, int y)
    {
        Coordinates = new Vector2Int(x, y);
        State = FarmTileState.NormalSoil;
        CropType = FarmCropType.None;
        GrowthStage = 0;
    }
}
