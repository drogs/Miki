﻿using IA;
using IA.Events;
using IA.Events.Attributes;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using Miki.Languages;
using Miki.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Modules
{
    [Module(Name = "settings")]
    internal class SettingsModule
    {
        [Command(Name = "toggledm")]
        public async Task ToggleDmAsync(EventContext e)
        {
            using (var context = new MikiContext())
            {
                Setting setting = await context.Settings.FindAsync(e.Author.Id.ToDbLong(), DatabaseEntityType.USER, DatabaseSettingId.PERSONALMESSAGE);

                if (setting == null)
                {
                    setting = context.Settings.Add(new Setting() { EntityId = e.Author.Id.ToDbLong(), EntityType = DatabaseEntityType.USER, IsEnabled = true, SettingId = DatabaseSettingId.PERSONALMESSAGE });
                }

                IDiscordEmbed embed = Utils.Embed;
                Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

                setting.IsEnabled = !setting.IsEnabled;
                string aa = (!setting.IsEnabled) ? locale.GetString("miki_generic_disabled") : locale.GetString("miki_generic_enabled");

                embed.Description = locale.GetString("miki_module_settings_dm", aa);
                embed.Color = (setting.IsEnabled) ? new IA.SDK.Color(1, 0, 0) : new IA.SDK.Color(0, 1, 0);

                await context.SaveChangesAsync();
                await embed.SendToChannel(e.Channel);
            }
        }

        [Command(Name = "toggleerrors")]
        public async Task ToggleErrors(EventContext e)
        {
            using (var context = new MikiContext())
            {
                Setting setting = await context.Settings.FindAsync(e.Author.Id.ToDbLong(), DatabaseEntityType.USER, DatabaseSettingId.ERRORMESSAGE);

                if (setting == null)
                {
                    setting = context.Settings.Add(new Setting() { EntityId = e.Author.Id.ToDbLong(), EntityType = DatabaseEntityType.USER, IsEnabled = true, SettingId = DatabaseSettingId.ERRORMESSAGE });
                }

                IDiscordEmbed embed = Utils.Embed;
                Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());
                setting.IsEnabled = !setting.IsEnabled;

                string aa = (!setting.IsEnabled) ? locale.GetString("miki_generic_disabled") : locale.GetString("miki_generic_enabled");

                embed.Description = locale.GetString("miki_module_settings_error_dm", aa);
                embed.Color = (setting.IsEnabled) ? new IA.SDK.Color(1, 0, 0) : new IA.SDK.Color(0, 1, 0);

                await context.SaveChangesAsync();
                await embed.SendToChannel(e.Channel);
            }
        }

        [Command(Name = "toggleguildnotifications", Aliases = new string[] { "tgn" }, Accessibility = EventAccessibility.ADMINONLY)]
        public async Task ToggleGuildNotifications(EventContext e)
        {
            using (var context = new MikiContext())
            {
                Setting setting = await context.Settings.FindAsync(e.Guild.Id.ToDbLong(), DatabaseEntityType.GUILD, DatabaseSettingId.CHANNELMESSAGE);

                if (setting == null)
                {
                    setting = context.Settings.Add(new Setting() { EntityId = e.Guild.Id.ToDbLong(), EntityType = DatabaseEntityType.GUILD, IsEnabled = true, SettingId = DatabaseSettingId.CHANNELMESSAGE });
                }

                IDiscordEmbed embed = Utils.Embed;
                Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());
                setting.IsEnabled = !setting.IsEnabled;

                string aa = (!setting.IsEnabled) ? locale.GetString("miki_generic_disabled") : locale.GetString("miki_generic_enabled");

                embed.Description = locale.GetString("miki_module_settings_guild_notifications", aa);
                embed.Color = (setting.IsEnabled) ? new IA.SDK.Color(1, 0, 0) : new IA.SDK.Color(0, 1, 0);

                await context.SaveChangesAsync();
                await embed.SendToChannel(e.Channel);
            }
        }

        [Command(Name = "showmodule")]
        public async Task ConfigAsync(EventContext e)
        {
            IModule module = e.commandHandler.GetModule(e.arguments);

            if (module != null)
            {
                IDiscordEmbed embed = Utils.Embed
                                           .SetTitle(e.arguments);

                string content = "";

                foreach (RuntimeCommandEvent ev in module.Events.OrderBy((x) => x.Name))
                {
                    content += (await ev.IsEnabled(e.Channel.Id) ? "<:iconenabled:341251534522286080>" : "<:icondisabled:341251533754728458>") + " " + ev.Name + "\n";
                }

                embed.AddInlineField("Events", content);

                content = "";

                foreach (IService ev in module.Services.OrderBy((x) => x.Name))
                {
                    content += (await ev.IsEnabled(e.Channel.Id) ? "<:iconenabled:341251534522286080>" : "<:icondisabled:341251533754728458>") + " " + ev.Name + "\n";
                }

                embed.AddInlineField("Services", content);

                await embed.SendToChannel(e.Channel);
            }
        }

        [Command(Name = "setlocale", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task SetLocale(EventContext e)
        {
            using (var context = new MikiContext())
            {
                ChannelLanguage language = await context.Languages.FindAsync(e.Channel.Id.ToDbLong());
                Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

                if (!Locale.LocaleNames.ContainsKey(e.arguments.ToLower()))
                {
                    await Utils.ErrorEmbed(locale, $"{e.arguments} is not a valid language. use `>listlocale` to check all languages available.").SendToChannel(e.Channel);
                    return;
                }

                if (language == null)
                {
                    language = context.Languages.Add(new ChannelLanguage() { EntityId = e.Channel.Id.ToDbLong(), Language = e.arguments.ToLower() });
                }

                language.Language = Locale.LocaleNames[e.arguments.ToLower()];
                await context.SaveChangesAsync();

                await Utils.SuccessEmbed(e.Channel.GetLocale(), $"Set locale to `{e.arguments}`\n\n**WARNING:** this feature is not fully implemented yet. use at your own risk.").SendToChannel(e.Channel);
            }
        }

        [Command(Name = "setprefix", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task PrefixAsync(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            if (string.IsNullOrEmpty(e.arguments))
            {
                await Utils.ErrorEmbed(locale, locale.GetString("miki_module_general_prefix_error_no_arg")).SendToChannel(e.Channel);
                return;
            }

            await PrefixInstance.Default.ChangeForGuildAsync(e.Guild.Id, e.arguments);

            IDiscordEmbed embed = Utils.Embed;
            embed.Title = locale.GetString("miki_module_general_prefix_success_header");
            embed.Description = locale.GetString("miki_module_general_prefix_success_message", e.arguments);

            embed.AddField(locale.GetString("miki_module_general_prefix_example_command_header"), $"{e.arguments}profile");

            await embed.SendToChannel(e.Channel);
        }

        [Command(Name = "listlocale", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task DoListLocale(EventContext e)
        {
            await Utils.Embed
                .SetTitle("Available locales")
                .SetDescription("`" + string.Join("`, `", Locale.LocaleNames.Keys) + "`")
                .SendToChannel(e.Channel.Id);
        }
    }
}