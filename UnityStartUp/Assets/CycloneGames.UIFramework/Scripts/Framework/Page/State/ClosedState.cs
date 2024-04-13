namespace CycloneGames.UIFramework
{
    public class ClosedState : UIPageState
    {
        public override void OnEnter(UIPage page)
        {
            UnityEngine.Debug.Log($"[PageState] Closed: {page.PageName}");
        }

        public override void OnExit(UIPage page)
        {
            
        }
    }
}