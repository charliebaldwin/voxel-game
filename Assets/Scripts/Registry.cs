using System.Collections.Generic;
using UnityEngine;

public class Registry : MonoBehaviour
{


    [SerializeField] private List<string> BaseBlockTypes = new List<string>();
    private static List<string> m_baseBlockTypes;

    [SerializeField] private Dictionary<string, VoxelState> DefaultVoxelStates;
    private static Dictionary<int, VoxelState> m_defaultVoxelStates;

    public static Registry Instance;
    private void SetInstance()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    private void RegisterData()
    {
        m_baseBlockTypes = new List<string>();
        foreach (string s in BaseBlockTypes)
        {
            if (s != null && s.Length > 0)
            {
                if (!m_baseBlockTypes.Contains(s))
                {
                    m_baseBlockTypes.Add(s);
                }
            }
        }

        m_defaultVoxelStates = new Dictionary<int, VoxelState>();
        foreach (KeyValuePair<string, VoxelState> kvp in DefaultVoxelStates)
        {
            int baseType = GetBlockBaseFromString(kvp.Key);
            if (baseType >= 0)
            {
                m_defaultVoxelStates.Add(baseType, kvp.Value);
            }
        }
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public int GetBlockBaseFromString(string blockBaseString)
    {
        if (!BaseBlockTypes.Contains(blockBaseString))
        {
            return -1;
        }
        return BaseBlockTypes.IndexOf(blockBaseString);
    }
    public int GetDefaultBlockState(int blockBase)
    {
        if (blockBase < 0 || blockBase >= BaseBlockTypes.Count)
        {
            return -1;
        }
        string blockBaseName = BaseBlockTypes[blockBase];
        if (!DefaultVoxelStates.ContainsKey(blockBaseName))
        {
            return -1;
        }
        return (int)DefaultVoxelStates[blockBaseName];
    }
}
