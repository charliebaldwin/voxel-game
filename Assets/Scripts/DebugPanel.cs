using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.Analytics.IAnalytic;
using static VoxelHelper;

public class DebugPanel : MonoBehaviour
{
    [Header("Panel Settings")]
    public float UpdateTime = 0.5f;
    private float updateCounter = 0f;
    public bool Enabled = false;

    [Header("GUI References")]
    public CanvasGroup CanvasGroup;
    public TextMeshProUGUI FPSText;
    public TextMeshProUGUI ItemNameText;
    public TextMeshProUGUI HitBlockIDText;
    public TextMeshProUGUI HitVoxelShapeText;
    public TextMeshProUGUI HitLocalVoxelText;
    public TextMeshProUGUI HitWorldVoxelText;
    public TextMeshProUGUI HitChunkText;
    public TextMeshProUGUI HitPositionText;
    public TextMeshProUGUI HitFaceText;
    public TextMeshProUGUI HitDistanceText;
    public TextMeshProUGUI PlayerLocalVoxelText;
    public TextMeshProUGUI PlayerWorldVoxelText;
    public TextMeshProUGUI PlayerChunkText;
    public TextMeshProUGUI PlayerPositionText;
    public TextMeshProUGUI PlayerFacingFloatText;
    public TextMeshProUGUI PlayerFacingIntText;

    [Header("Variables")]
    public static Item EquippedItem;
    public static VoxelHitInfo LastHitInfo;
    public static Vector3 PlayerPosition;
    public static Vector3Int PlayerLocalVoxel;
    public static Vector3Int PlayerWorldVoxel;
    public static Vector3Int PlayerChunk;
    public static Vector3 PlayerForward;
    public static OrthoNormal PlayerForwardOrtho;

    
    public void TogglePanel(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Enabled = !Enabled;
            CanvasGroup.alpha = Enabled ? 1f : 0f;
            updateCounter = UpdateTime;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Enabled)
        {

            updateCounter += Time.deltaTime;
            if (updateCounter >= UpdateTime)
            {
                FPSText.text = (1f / Time.deltaTime).ToString().Substring(0, 5) + "fps";
                if (EquippedItem != null)
                    ItemNameText.text = EquippedItem.Name;

                if (LastHitInfo.didHit)
                {
                    HitBlockIDText.text = LastHitInfo.voxel.BlockID.ToString();
                    HitVoxelShapeText.text = $"({LastHitInfo.voxel.Shape.ToString()})";
                    HitLocalVoxelText.text = LastHitInfo.localVoxelPos.ToString();
                    HitWorldVoxelText.text = LastHitInfo.voxelPos.ToString();
                    HitChunkText.text = $"[{LastHitInfo.chunkPos.x}, {LastHitInfo.chunkPos.z}]";
                    HitPositionText.text = LastHitInfo.hitPos.ToString();
                    HitDistanceText.text = LastHitInfo.distance.ToString();
                    HitFaceText.text = NormalVectorToText(LastHitInfo.hitNormal);
                }
                else
                {
                    HitBlockIDText.text = "None";
                    HitLocalVoxelText.text = "-";
                    HitWorldVoxelText.text = "-";
                    HitChunkText.text = "-";
                    HitPositionText.text = "-";
                    HitDistanceText.text = "-";
                    HitFaceText.text = "-";
                }

                PlayerWorldVoxel = VoxelHelper.SnapToGrid(PlayerPosition);
                PlayerChunk = VoxelHelper.FindContainingChunk(PlayerWorldVoxel, VoxelWorld.Instance.ChunkSize);
                PlayerLocalVoxel = VoxelHelper.WorldToLocal(PlayerWorldVoxel, PlayerChunk, VoxelWorld.Instance.ChunkSize);
                PlayerPositionText.text = PlayerPosition.ToString();
                PlayerWorldVoxelText.text = PlayerWorldVoxel.ToString();
                PlayerLocalVoxelText.text = PlayerLocalVoxel.ToString();
                PlayerChunkText.text = $"[{PlayerChunk.x}, {PlayerChunk.z}]";
                PlayerFacingFloatText.text = PlayerForward.ToString();
                PlayerFacingIntText.text = PlayerForwardOrtho.ToString();

                updateCounter = 0f;
            }
        }


    }

    
}
