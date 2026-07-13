using HasbeMaal.Core.Domain;
using HasbeMaal.Core.Planning;
using HasbeMaal.Presentation.ViewModels;

namespace HasbeMaal.Mobile.Tests;

[TestClass]
public sealed class GoalsViewModelTests
{
    [TestMethod]
    public void Constructor_HasPredictableEmptyState()
    {
        var viewModel = new GoalsViewModel(new FakeFinancialGoalRepository());

        Assert.IsTrue(viewModel.IsEmpty);
        Assert.IsFalse(viewModel.IsLoading);
        Assert.IsEmpty(viewModel.Items);
        Assert.IsFalse(viewModel.HasStatus);
    }

    [TestMethod]
    public async Task LoadAsync_ProjectsMonthlyContributionForEachGoal()
    {
        var goal = new FinancialGoal(
            "School admission",
            new MoneyAmount(80000m),
            new MoneyAmount(20000m),
            FutureDate(6),
            "Kid school");
        var viewModel = new GoalsViewModel(new FakeFinancialGoalRepository(goal));

        await viewModel.LoadAsync();

        var item = Assert.ContainsSingle(viewModel.Items);
        Assert.AreEqual(goal.Id, item.Id);
        Assert.AreEqual("School admission", item.Name);
        Assert.AreEqual("Target 80000.00 INR", item.TargetText);
        Assert.AreEqual("Saved 20000.00 INR", item.CurrentText);
        Assert.AreEqual("60000.00 INR to go", item.RemainingText);
        Assert.IsTrue(item.MonthlyContributionText.StartsWith("Set aside ", StringComparison.Ordinal));
        Assert.IsTrue(item.StatusText.Contains("month", StringComparison.Ordinal));
        Assert.IsFalse(viewModel.IsEmpty);
    }

    [TestMethod]
    public async Task LoadAsync_ReachedGoal_ShowsNoMonthlySetAside()
    {
        var goal = new FinancialGoal(
            "Funded goal",
            new MoneyAmount(50000m),
            new MoneyAmount(50000m),
            FutureDate(3),
            "Already funded");
        var viewModel = new GoalsViewModel(new FakeFinancialGoalRepository(goal));

        await viewModel.LoadAsync();

        var item = Assert.ContainsSingle(viewModel.Items);
        Assert.AreEqual("Goal reached", item.StatusText);
        Assert.AreEqual("No monthly set-aside needed", item.MonthlyContributionText);
        Assert.AreEqual(1m, item.Progress);
    }

    [TestMethod]
    public async Task AddGoalCommand_WithValidInputs_PersistsGoalAndResetsForm()
    {
        var repository = new FakeFinancialGoalRepository();
        var viewModel = new GoalsViewModel(repository)
        {
            NewGoalName = "Car fund",
            NewGoalPurpose = "New car",
            NewGoalTargetAmount = "400000",
            NewGoalCurrentAmount = "50000",
            NewGoalTargetDate = DateTime.Today.AddMonths(12),
        };

        await ((AsyncRelayCommand)viewModel.AddGoalCommand).ExecuteAsync();

        var saved = Assert.ContainsSingle(repository.Goals);
        Assert.AreEqual("Car fund", saved.Name);
        Assert.AreEqual("New car", saved.Purpose);
        Assert.AreEqual(new MoneyAmount(400000m), saved.TargetAmount);
        Assert.AreEqual(new MoneyAmount(50000m), saved.CurrentAmount);
        Assert.ContainsSingle(viewModel.Items);
        Assert.AreEqual("Goal saved.", viewModel.StatusText);
        Assert.AreEqual(string.Empty, viewModel.NewGoalName);
        Assert.AreEqual(string.Empty, viewModel.NewGoalTargetAmount);
    }

    [TestMethod]
    public void AddGoalCommand_CannotExecuteWithoutNameAndTarget()
    {
        var viewModel = new GoalsViewModel(new FakeFinancialGoalRepository());

        Assert.IsFalse(viewModel.AddGoalCommand.CanExecute(null));

        viewModel.NewGoalName = "Emergency fund";
        viewModel.NewGoalTargetAmount = "150000";

        Assert.IsTrue(viewModel.AddGoalCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task AddGoalCommand_SavedMoreThanTarget_ReportsErrorAndDoesNotPersist()
    {
        var repository = new FakeFinancialGoalRepository();
        var viewModel = new GoalsViewModel(repository)
        {
            NewGoalName = "Over-funded",
            NewGoalTargetAmount = "1000",
            NewGoalCurrentAmount = "2000",
            NewGoalTargetDate = DateTime.Today.AddMonths(6),
        };

        await ((AsyncRelayCommand)viewModel.AddGoalCommand).ExecuteAsync();

        Assert.IsEmpty(repository.Goals);
        Assert.AreEqual("Saved amount cannot be more than the target.", viewModel.StatusText);
    }

    [TestMethod]
    public async Task AddEmergencyFundAsync_ComputesTargetFromMonthsOfCover()
    {
        var repository = new FakeFinancialGoalRepository();
        var viewModel = new GoalsViewModel(repository);

        await viewModel.AddEmergencyFundAsync(
            essentialMonthlyExpenses: 30000m,
            monthsOfCover: 6,
            currentSaved: 45000m,
            targetDate: FutureDate(12));

        var saved = Assert.ContainsSingle(repository.Goals);
        Assert.AreEqual("Emergency fund", saved.Name);
        Assert.AreEqual(new MoneyAmount(180000m), saved.TargetAmount);
        Assert.AreEqual(new MoneyAmount(45000m), saved.CurrentAmount);
        Assert.AreEqual("6 months of essential expenses", saved.Purpose);
        Assert.ContainsSingle(viewModel.Items);
    }

    [TestMethod]
    public async Task DeleteGoalAsync_RemovesGoalAndRefreshes()
    {
        var goal = new FinancialGoal(
            "Emergency fund",
            new MoneyAmount(150000m),
            new MoneyAmount(25000m),
            FutureDate(9),
            "Household reserve");
        var repository = new FakeFinancialGoalRepository(goal);
        var viewModel = new GoalsViewModel(repository);
        await viewModel.LoadAsync();

        await viewModel.DeleteGoalAsync(goal.Id);

        Assert.IsEmpty(repository.Goals);
        Assert.IsEmpty(viewModel.Items);
        Assert.IsTrue(viewModel.IsEmpty);
    }

    private static DateOnly FutureDate(int monthsFromToday)
    {
        return DateOnly.FromDateTime(DateTime.Today.AddMonths(monthsFromToday));
    }

    private sealed class FakeFinancialGoalRepository : IFinancialGoalRepository
    {
        private readonly List<FinancialGoal> goals = [];

        public FakeFinancialGoalRepository(params FinancialGoal[] seed)
        {
            goals.AddRange(seed);
        }

        public IReadOnlyList<FinancialGoal> Goals => goals;

        public Task<IReadOnlyList<FinancialGoal>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<FinancialGoal>>(goals.ToArray());
        }

        public Task SaveAsync(FinancialGoal goal, CancellationToken cancellationToken = default)
        {
            var index = goals.FindIndex(existing => existing.Id == goal.Id);
            if (index >= 0)
            {
                goals[index] = goal;
            }
            else
            {
                goals.Add(goal);
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            goals.RemoveAll(existing => existing.Id == id);
            return Task.CompletedTask;
        }
    }
}
