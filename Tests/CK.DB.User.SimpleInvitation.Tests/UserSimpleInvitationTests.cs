using CK.Core;
using CK.SqlServer;
using CK.Testing;
using Shouldly;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.DB.User.SimpleInvitation.Tests;

[TestFixture]
public class UserSimpleInvitationTests
{

    [TestCase( true )]
    [TestCase( false )]
    public void creating_starting_and_destroying_an_invitation( bool firstInvitationSent )
    {
        var inv = SharedEngine.Map.StObjs.Obtain<UserSimpleInvitationTable>();
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var info = inv.CreateInfo();
            info.EMail = Guid.NewGuid().ToString( "N" ) + "@abc.com";
            info.ExpirationDateUtc = DateTime.UtcNow.AddHours( 1 );
            var r = inv.Create( ctx, 1, info, firstInvitationSent );
            r.Success.ShouldBeTrue();
            r.InvitationId.ShouldBeGreaterThan( 0 );
            r.InvitationToken.ShouldNotBeEmpty();
            var rs = inv.StartResponse( ctx, 1, r.InvitationToken );
            rs.IsValid().ShouldBeTrue();
            rs.InvitationId.ShouldBe( r.InvitationId );
            rs.Options.ShouldBeNull();
            if( firstInvitationSent )
            {
                rs.InvitationSendCount.ShouldBe( 1 );
                rs.LastInvitationSendDate.ShouldBe( DateTime.UtcNow, tolerance: TimeSpan.FromSeconds( 1 ) );
            }
            else
            {
                rs.InvitationSendCount.ShouldBe( 0 );
                rs.LastInvitationSendDate.ShouldBe( Util.UtcMinValue );
            }
            inv.Destroy( ctx, 1, r.InvitationId );
        }
    }

    [Test]
    public void creating_an_invitation_to_an_existing_mail_fails()
    {
        var inv = SharedEngine.Map.StObjs.Obtain<UserSimpleInvitationTable>();
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var info = inv.CreateInfo();
            info.EMail = Guid.NewGuid().ToString( "N" ) + "@abc.com";
            info.ExpirationDateUtc = DateTime.UtcNow.AddHours( 1 );
            var r1 = inv.Create( ctx, 1, info );
            r1.Success.ShouldBeTrue();

            var r2 = inv.Create( ctx, 1, info );
            r2.Success.ShouldBeFalse();
        }
    }

    [Test]
    public void unexisting_invitation_token_does_not_start_an_invitation()
    {
        var inv = SharedEngine.Map.StObjs.Obtain<UserSimpleInvitationTable>();
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            string token = $"3712.{Guid.NewGuid()}";
            var rs = inv.StartResponse( ctx, 1, token );
            rs.InvitationId.ShouldBe( 0 );
            rs.ExpirationDateUtc.ShouldBe( Util.UtcMinValue );
            rs.EMail.ShouldBeNull();
            rs.InvitationToken.ShouldBe( token );
            rs.Options.ShouldBeNull();
        }
    }

    [Test]
    public void invalid_invitation_token_raises_an_exception()
    {
        var inv = SharedEngine.Map.StObjs.Obtain<UserSimpleInvitationTable>();
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            string token = $"This is not.A valid token";
            Util.Invokable( () => inv.StartResponse( ctx, 1, token ) ).ShouldThrow<SqlDetailedException>();
        }
    }

    [Test]
    public void invitation_expiration_is_boosted_by_at_least_5_minutes_when_invitation_starts()
    {
        var inv = SharedEngine.Map.StObjs.Obtain<UserSimpleInvitationTable>();
        using( var ctx = new SqlStandardCallContext() )
        {
            var info = inv.CreateInfo();
            info.EMail = Guid.NewGuid().ToString( "N" ) + "@abc.com";
            info.ExpirationDateUtc = DateTime.UtcNow.AddMinutes( 2 );
            var r1 = inv.Create( ctx, 1, info );
            r1.Success.ShouldBeTrue();

            var boosted = info.ExpirationDateUtc.AddMinutes( 5 );
            var startInfo = inv.StartResponse( ctx, 1, r1.InvitationToken );
            startInfo.InvitationId.ShouldBeGreaterThan( 0 );
            boosted.ShouldBeLessThan( startInfo.ExpirationDateUtc );
        }
    }

    [Test]
    public async Task invitation_expiration_Async()
    {
        var inv = SharedEngine.Map.StObjs.Obtain<UserSimpleInvitationTable>();
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var info = inv.CreateInfo();
            info.EMail = Guid.NewGuid().ToString( "N" ) + "@abc.com";
            info.ExpirationDateUtc = DateTime.UtcNow.AddMilliseconds( 500 );
            var r1 = await inv.CreateAsync( ctx, 1, info );
            r1.Success.ShouldBeTrue();

            await Task.Delay( 550 );
            var startInfo = await inv.StartResponseAsync( ctx, 1, r1.InvitationToken );
            startInfo.IsValid().ShouldBeFalse();
        }
    }


}
