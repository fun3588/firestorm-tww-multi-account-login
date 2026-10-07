namespace logingui;

public static class AccountDeletion
{
    public static bool Delete(List<Account> accounts, Account selected, Func<int, bool> confirm)
    {
        if (!confirm(1) || !confirm(2)) return false;
        accounts.RemoveAll(a => a.Email.Equals(selected.Email, StringComparison.OrdinalIgnoreCase));
        AccountStore.Save(accounts);
        return true;
    }
}
