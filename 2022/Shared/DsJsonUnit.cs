using System.Collections.Generic;
using System.Numerics;

namespace sc2dsstats._2022.Shared
{
    public class DsJsonEnt
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Race { get; set; }
    }

    public class DsJsonObjective
    {
        public int ID { get; set; }
        public Vector2 ObjectivePlanetaryFortress { get; set; } = Vector2.Zero;
        public Vector2 ObjectiveNexus { get; set; } = Vector2.Zero;
        public Vector2 ObjectiveBunker { get; set; } = Vector2.Zero;
        public Vector2 ObjectivePhotonCannon { get; set; } = Vector2.Zero;
        public Vector2 Center { get; set; } = Vector2.Zero;

        public KeyValuePair<Vector2, Vector2> LineT1 { get; set; } = new KeyValuePair<Vector2, Vector2>(Vector2.Zero, Vector2.Zero);
        public KeyValuePair<Vector2, Vector2> LineT2 { get; set; } = new KeyValuePair<Vector2, Vector2>(Vector2.Zero, Vector2.Zero);
    }
}
