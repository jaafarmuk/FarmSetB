using UnityEngine;

[System.Serializable]
public class FarmCropDefinition
{
    public FarmCropType CropType;
    public Sprite[] StageSprites = new Sprite[5];

    public Sprite GetStageSprite(int growthStage)
    {
        if (StageSprites == null || growthStage < 0 || growthStage >= StageSprites.Length)
        {
            return null;
        }

        return StageSprites[growthStage];
    }
}
