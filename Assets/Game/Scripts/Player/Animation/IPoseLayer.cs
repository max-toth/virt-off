using Game.Scripts.Player.Animation.Models;

namespace Game.Scripts.Player.Animation
{
    /// <summary>Слой процедурной позы. Слои применяются по порядку поверх исходной позы, поэтому порядок важен.</summary>
    public interface IPoseLayer
    {
        void Apply(WalkRig rig, in GaitState gait);
    }
}
