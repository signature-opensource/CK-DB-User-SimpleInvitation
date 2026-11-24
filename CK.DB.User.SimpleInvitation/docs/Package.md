Implements the `CK.tUserSimpleInvitation` table: invitations sent by a user to an email address (one invitation per
email), each with an expiration date and a token to respond to it.

`UserSimpleInvitationTable` creates an invitation (`UserSimpleInvitationTable.CreateAsync`), starts the response to it
from its token (`UserSimpleInvitationTable.StartResponseAsync`) and destroys it once it succeeded or is canceled
(`UserSimpleInvitationTable.DestroyAsync`); `IUserSimpleInvitationInfo` is the invitation poco.
