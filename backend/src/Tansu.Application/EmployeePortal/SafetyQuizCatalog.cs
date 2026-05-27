namespace Tansu.Application.EmployeePortal;

public static class SafetyQuizCatalog
{
    public static IReadOnlyList<SafetyQuizQuestionDto> Questions { get; } =
    [
        new("q1", "Обязаны ли вы использовать средства индивидуальной защиты (каска, спецобувь) на объекте?",
        [
            new("a", "Да, всегда на рабочей зоне"),
            new("b", "Только при работе на высоте"),
            new("c", "Нет, если работаю недолго")
        ]),
        new("q2", "Можно ли начинать работу без прохождения инструктажа по ТБ?",
        [
            new("a", "Да, если опытный специалист"),
            new("b", "Нет, только после инструктажа"),
            new("c", "Да, если субподрядчик разрешил")
        ]),
        new("q3", "Что делать при обнаружении опасной ситуации на объекте?",
        [
            new("a", "Продолжить работу и сообщить в конце смены"),
            new("b", "Немедленно остановить работу и сообщить прорабу/охране"),
            new("c", "Исправить самостоятельно без уведомления")
        ]),
        new("q4", "Разрешено ли нахождение на объекте без действующего QR-пропуска после Face ID?",
        [
            new("a", "Да, если меня знают"),
            new("b", "Нет, допуск только после проверки QR и Face ID"),
            new("c", "Да, в первый рабочий день")
        ]),
        new("q5", "Нужно ли соблюдать указания ответственного за ТБ на объекте?",
        [
            new("a", "Да, обязательно"),
            new("b", "Только при проверке"),
            new("c", "Нет, если мешают работе")
        ])
    ];

    private static readonly Dictionary<string, string> CorrectAnswers = new(StringComparer.Ordinal)
    {
        ["q1"] = "a",
        ["q2"] = "b",
        ["q3"] = "b",
        ["q4"] = "b",
        ["q5"] = "a"
    };

    public static (int Score, int Total, bool Passed) Grade(IReadOnlyDictionary<string, string> answers)
    {
        var total = Questions.Count;
        var score = 0;
        foreach (var (questionId, correct) in CorrectAnswers)
        {
            if (answers.TryGetValue(questionId, out var given)
                && string.Equals(given, correct, StringComparison.Ordinal))
            {
                score++;
            }
        }

        var passed = score == total;
        return (score, total, passed);
    }
}
