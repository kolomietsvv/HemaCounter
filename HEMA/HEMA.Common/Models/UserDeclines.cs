namespace HEMA
{
	public struct UserDeclines
    {
        public bool UserDeclinedDoubleHitsFinish;
        public bool UserDeclinedTimeFinish;

        public void Reset()
        {
            UserDeclinedDoubleHitsFinish = false;
            UserDeclinedTimeFinish = false;
        }
    }
}
