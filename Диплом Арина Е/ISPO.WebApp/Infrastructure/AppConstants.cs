namespace ISPO.WebApp.Infrastructure;

public static class AppConstants
{
    public static class LessonLiveStatus
    {
        public const string Planned = "planned";
        public const string Live = "live";
        public const string Finished = "finished";
        public static readonly string[] All = [Planned, Live, Finished];
    }

    public static class LessonMaterialType
    {
        public const string Task = "task";
        public const string Methodic = "methodic";
        public const string Reference = "reference";
        public static readonly string[] All = [Task, Methodic, Reference];
    }

    public static class SubmissionStatus
    {
        public const string New = "new";
        public const string InReview = "in_review";
        public const string Revision = "revision";
        public const string Accepted = "accepted";
        public static readonly string[] All = [New, InReview, Revision, Accepted];
    }
}
