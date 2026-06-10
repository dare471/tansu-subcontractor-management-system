namespace Tansu.Application.EmployeePortal;

public static class SafetyQuizCatalog
{
    public static IReadOnlyList<SafetyQuizQuestionDto> GetQuestions(string? locale = null) =>
        locale?.ToLowerInvariant() switch
        {
            "kk" => QuestionsKk,
            "en" => QuestionsEn,
            _ => QuestionsRu
        };

    private static IReadOnlyList<SafetyQuizQuestionDto> QuestionsRu { get; } =
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

    private static IReadOnlyList<SafetyQuizQuestionDto> QuestionsEn { get; } =
    [
        new("q1", "Must you use PPE (helmet, safety shoes) on site?",
        [new("a", "Yes, always in the work zone"), new("b", "Only when working at height"), new("c", "No, for short tasks")]),
        new("q2", "May you start work without safety briefing?",
        [new("a", "Yes, if experienced"), new("b", "No, only after briefing"), new("c", "Yes, if subcontractor allows")]),
        new("q3", "What to do when you spot a hazard?",
        [new("a", "Continue and report at end of shift"), new("b", "Stop work and notify supervisor/security"), new("c", "Fix it alone without reporting")]),
        new("q4", "Is site access allowed without valid QR pass after Face ID?",
        [new("a", "Yes, if known"), new("b", "No, only after QR and Face ID check"), new("c", "Yes, on first day")]),
        new("q5", "Must you follow safety officer instructions?",
        [new("a", "Yes, always"), new("b", "Only during inspection"), new("c", "No, if it slows work")])
    ];

    private static IReadOnlyList<SafetyQuizQuestionDto> QuestionsKk { get; } =
    [
        new("q1", "Нысанда ЖҚҚ (каска, арнайы аяқ киім) міндетті ме?",
        [new("a", "Иә, жұмыс аймағында әрқашан"), new("b", "Тек биіктікте"), new("c", "Жоқ, қысқа уақытқа")]),
        new("q2", "Қауіпсіздік нұсқауынсыз жұмысты бастауға бола ма?",
        [new("a", "Иә, тәжірибелі болсаң"), new("b", "Жоқ, тек нұсқаудан кейін"), new("c", "Иә, субподрядчик рұқсат етсе")]),
        new("q3", "Қауіпті жағдайды байқасаң не істейсің?",
        [new("a", "Жұмысты жалғастырып, аяқтауға хабарла"), new("b", "Жұмысты тоқтатып, басшыға/күзетке хабарла"), new("c", "Өзің жөндейсің, хабарламай")]),
        new("q4", "Face ID кейін QR-сыз объектіге кіруге бола ма?",
        [new("a", "Иә, таныс болсаң"), new("b", "Жоқ, тек QR және Face ID тексеруден кейін"), new("c", "Иә, алғашқы күні")]),
        new("q5", "Қауіпсіздік жауаптысының нұсқауын орындау керек пе?",
        [new("a", "Иә, міндетті"), new("b", "Тек тексеру кезінде"), new("c", "Жоқ, кедергі болса")])
    ];

    public static IReadOnlyList<SafetyQuizQuestionDto> Questions => QuestionsRu;

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
        var total = QuestionsRu.Count;
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
