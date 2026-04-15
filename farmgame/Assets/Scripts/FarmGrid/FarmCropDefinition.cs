using UnityEngine;

[System.Serializable]
public class FarmCropDefinition
{
    public FarmCropType CropType;
    public ItemData HarvestItem;
    public Sprite Stage00Sprite;
    public Sprite Stage01Sprite;
    public Sprite Stage02Sprite;
    public Sprite Stage03Sprite;
    public Sprite Stage04Sprite;

    public Sprite GetStageSprite(int growthStage)
    {
        return growthStage switch
        {
            0 => Stage00Sprite,
            1 => Stage01Sprite,
            2 => Stage02Sprite,
            3 => Stage03Sprite,
            4 => Stage04Sprite,
            _ => null
        };
    }
}
