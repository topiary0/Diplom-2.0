namespace ISPO.WebApp.Infrastructure;

public static class UiText
{
    public static string Role(string value) => value?.ToLowerInvariant() switch
    {
        "admin" => "Администратор",
        "teacher" => "Преподаватель",
        "student" => "Ученик",
        _ => "Не указан"
    };

    public static string LessonStatus(string value) => value?.ToLowerInvariant() switch
    {
        "planned" => "Запланировано",
        "live" => "Идет урок",
        "finished" => "Завершено",
        _ => "Не указан"
    };

    public static string LessonStatusBadgeClass(string value) => value?.ToLowerInvariant() switch
    {
        "live" => "ok",
        _ => "warn"
    };

    public static string SubmissionStatus(string value) => value?.ToLowerInvariant() switch
    {
        "new" => "Новая",
        "in_review" => "На проверке",
        "revision" => "Доработка",
        "accepted" => "Принята",
        _ => "Не указан"
    };

    public static string SubmissionStatusBadgeClass(string value) => value?.ToLowerInvariant() switch
    {
        "accepted" => "ok",
        _ => "warn"
    };

    public static string EnrollmentStatus(string value) => value?.ToLowerInvariant() switch
    {
        "active" => "Активно",
        "completed" => "Завершено",
        "cancelled" => "Отменено",
        _ => "Не указан"
    };

    public static string MaterialType(string value) => value?.ToLowerInvariant() switch
    {
        "task" => "Задание",
        "methodic" => "Методичка",
        "reference" => "Справка",
        _ => "Материал"
    };
}
