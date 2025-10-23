namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// Basic interface for genetic profiles to avoid direct Chimera dependency in Core
    /// </summary>
    public interface IGeneticProfile
    {
        string GetGeneticId();
        float GetOverallFitness();
        float GetTraitValue(string traitName);

        // Properties for direct access
        System.Collections.Generic.Dictionary<string, float> Traits { get; }
        float OverallFitness { get; set; }
    }

    /// <summary>
    /// Simple implementation for core systems that don't need full Chimera integration
    /// </summary>
    public class BasicGeneticProfile : IGeneticProfile
    {
        public string GeneticId { get; set; } = System.Guid.NewGuid().ToString();
        public float OverallFitness { get; set; } = 0.5f;
        public System.Collections.Generic.Dictionary<string, float> Traits { get; set; } = new();

        public string GetGeneticId() => GeneticId;
        public float GetOverallFitness() => OverallFitness;

        public float GetTraitValue(string traitName)
        {
            return Traits.TryGetValue(traitName, out float value) ? value : 0.5f;
        }
    }
}