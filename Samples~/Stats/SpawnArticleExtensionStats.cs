using Quantum;

namespace HnSF.core.state.actions
{
    public unsafe partial class SpawnArticle : HNSFStateAction
    {
        partial void ConfigureArticleStats(Frame frame, EntityRef articleOwner, EntityRef article)
        {
            frame.Remove<ActorCombatStats>(article);

            frame.Add(article, new ActorCombatStatsFrom()
            {
                actorCombatStatEntityRef = articleOwner
            });
        }
    }
}