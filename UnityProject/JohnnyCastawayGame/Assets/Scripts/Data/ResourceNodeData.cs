namespace JohnnyGame.Data
{
    [System.Serializable]
    public class ResourceNodeData
    {
        // e.g. "resource.wood", "resource.food", "resource.scrap"
        public string resourceId;
        public int x;
        public int y;
        public float amount;
        public float maxAmount;
    }
}
