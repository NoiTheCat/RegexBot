# RegexBot
**This branch is still a major work in progress, and is highly incomplete. See the legacy branch for the current working version.**

RegexBot is a Discord moderation bot framework of sorts, inspired by the terrible state of Discord moderation tools a few years ago
combined with my tendency to overengineer things until they into pseudo-libraries of their own right.

### Features:
* Provides a sort of in-between interface to Discord.Net that allows modules to be written for it, its benefits being:
  * Putting together disparate bot features under a common interface.
  * Reducing duplicate code potentially leading to an inconsistent user experience.
* Versatile JSON-based configuration.
* High detail logging and record-keeping prevents gaps in moderation that might occur with large public bots.

This repository also contains...

# RegexBot-Modules
An optional set of features to add to RegexBot, some of them inspired by Reddit's Automoderator.

This module provides a number of features to assist in watching over the tedious details in a busy server with no hidden details,
arbitrary restrictions, or unmodifiable behavior. Its configuration allows for a very high level of flexibility, ensuring that the bot
behaves in accordance to the exact needs of your server.

### Features:
* Create rules based on regular expression patterns
  * Follow up with custom responses ranging from sending a DM to disciplinary action
* Create pattern-based triggers to provide information and fun to your users
  * Adjustable rate limits per-trigger to prevent spam
  * Specify multiple different responses to display at random when triggered
  * Make things interesting by setting triggers that only activate at random
* Individual rules and triggers can be whitelisted or blacklisted per-user, per-channel, or per-role
  * Exemptions to these filters can be applied for additional flexibility

## Documentation
Coming soon?
