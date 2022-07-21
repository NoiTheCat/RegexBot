# RegexBot
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/J3J65TW2E)

RegexBot is a Discord moderation bot framework of sorts, inspired by the terrible state of Discord moderation tools a few years ago
combined with my tendency to overengineer things until they into pseudo-libraries of their own right.

This bot includes a number of features which assist in handling the tedious details in a busy server with the goal of minimizing
the occurrence of hidden details, arbitrary restrictions, or annoyingly unmodifiable behavior. Its configuration allows for a very high
level of flexibility, ensuring that the bot behaves in accordance to the exact needs of your server without compromise.

### Features
* Create rules based on regular expression patterns
  * Follow up with custom responses ranging from sending a DM to disciplinary action
* Create pattern-based triggers to provide information and fun to your users
  * Adjustable rate limits per-trigger to prevent spam
  * Specify multiple different responses to display at random when triggered
  * Make things interesting by setting triggers that only activate at random
* Individual rules and triggers can be whitelisted or blacklisted per-user, per-channel, or per-role
  * Exemptions to these filters can be applied for additional flexibility
* High detail logging and record-keeping prevents gaps in moderation that might occur with large public bots.

### Modules
As mentioned above, this bot also serves as a framework of sorts, allowing others to write their own modules and expand
the bot's feature set ever further. Its benefits are:
* Putting together disparate bot features under a common, consistent interface.
* Reducing duplicate code potentially leading to an inconsistent user experience.
* Versatile JSON-based configuration.

## User documentation
Coming soon?
