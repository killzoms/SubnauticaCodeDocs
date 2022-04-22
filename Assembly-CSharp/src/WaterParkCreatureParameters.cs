namespace AssemblyCSharp
{
    public class WaterParkCreatureParameters
    {
        public bool isPickupableOutside;

        public float initialSize;

        public float maxSize;

        public float outsideSize;

        public float growingPeriod;

        public WaterParkCreatureParameters(float initialSize, float maxSize, float outsideSize, float growingPeriodInDays, bool isPickupableOutside = true)
        {
            this.initialSize = initialSize;
            this.maxSize = maxSize;
            this.outsideSize = outsideSize;
            growingPeriod = growingPeriodInDays * 1200f;
            this.isPickupableOutside = isPickupableOutside;
        }

        public static WaterParkCreatureParameters GetDefaultValue()
        {
            return new WaterParkCreatureParameters(0.1f, 0.6f, 1f, 1f);
        }
    }
}
