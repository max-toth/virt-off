using Game.Scripts.Player.Animation.Models;

namespace Game.Scripts.Player.Animation
{
    public interface IPoseLayer
    {
        void Apply(WalkRig rig, in GaitState gait);
    }
}
