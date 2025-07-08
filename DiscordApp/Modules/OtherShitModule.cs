using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Keep in mind your module **must** be public and inherit ModuleBase.
// If it isn't, it will not be discovered by AddModulesAsync!
// Create a module with no prefix
public class OtherShitModule : ModuleBase<SocketCommandContext>
{

    [Command("say")]
    [Summary("Echoes a message.")]
    public Task SayAsync(string echo)
        => ReplyAsync(echo);

}

// 'sample' prefix
[Group("sample")]
public class SampleModule : ModuleBase<SocketCommandContext>
{
    [Command("square")]
    [Summary("Squares a number.")]
    public async Task SquareAsync(
        [Summary("The number to square.")]
        int num)
    {
        await Context.Channel.SendMessageAsync($"{num}^2 = {Math.Pow(num, 2)}");
    }

    // ~sample userinfo --> foxbot#0282
    // ~sample userinfo @Khionu --> Khionu#8708
    // ~sample userinfo Khionu#8708 --> Khionu#8708
    // ~sample userinfo Khionu --> Khionu#8708
    // ~sample userinfo 96642168176807936 --> Khionu#8708
    // ~sample whois 96642168176807936 --> Khionu#8708
    [Command("userinfo")]
    [Alias("user", "whois")]
    [Summary
    ("Returns info about the current user, or the user parameter, if one passed.")]
    public async Task UserInfoAsync(
        [Summary("The (optional) user to get info from")]
        SocketUser? user = null)
    {
        var userInfo = user ?? Context.Client.CurrentUser;

        var avatarUrl = userInfo.GetAvatarUrl();
        await ReplyAsync("", false, new EmbedBuilder()
            .WithImageUrl(avatarUrl)
            .Build());

        await ReplyAsync($"{userInfo.Username}#{userInfo.Discriminator}{Environment.NewLine}");
    }
}
