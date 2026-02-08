namespace Relate.Smtp.ImapHost.Protocol;

/// <summary>
/// IMAP4rev2 protocol states (RFC 9051)
/// </summary>
public enum ImapState
{
    /// <summary>
    /// Initial state after connection. Only LOGIN command allowed.
    /// </summary>
    NotAuthenticated,

    /// <summary>
    /// After successful authentication. Can SELECT/EXAMINE mailboxes.
    /// </summary>
    Authenticated,

    /// <summary>
    /// A mailbox is selected. Full message access available.
    /// </summary>
    Selected,

    /// <summary>
    /// Client has issued LOGOUT. Connection closing.
    /// </summary>
    Logout
}
