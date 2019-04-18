// Sitecore.Data.LanguageFallback.LanguageFallbackFieldValuesProvider
using Sitecore;
using Sitecore.Caching;
using Sitecore.Common;
using Sitecore.Configuration;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.LanguageFallback;
using Sitecore.Data.Managers;
using Sitecore.Globalization;
using Sitecore.Sites;

namespace Sitecore.Support.Data.LanguageFallback
{
    public class LanguageFallbackFieldValuesProvider : Sitecore.Data.LanguageFallback.LanguageFallbackFieldValuesProvider
    {
    /// <summary>
    /// Determines whether valid is valid to have fallback value.
    /// </summary>
    /// <param name="field">The field to check.</param>
    /// <returns></returns>
    public override bool IsValidForFallback(Field field)
    {
      var switcherValue = LanguageFallbackFieldSwitcher.CurrentValue;
      if (switcherValue == false)
      {
        return false;
      }

      SiteContext site;
      if (switcherValue != true && ((site = Context.Site) == null || !site.SiteInfo.EnableFieldLanguageFallback))
      {
        return false;
      }

      bool result = true;

      var item = field.Item;
      var key = new IsLanguageFallbackValidCacheKey(item.ID.ToString(), field.ID.ToString(), field.Database.Name, field.Language.Name);
      var cachedResult = this.GetFallbackIsValidValueFromCache(field, key);
      if (cachedResult != null)
      {
        result = (string)cachedResult == "1";
        return result;
      }

      Language fallbackLanguage = LanguageFallbackManager.GetFallbackLanguage(item.Language, item.Database, item.ID);
      if (fallbackLanguage == null || string.IsNullOrEmpty(fallbackLanguage.Name))
      {
        result = false;
      }
      // shared fields cannot have language fallback by definition
      else if (field.Shared)
      {
        result = false;
      }
      else if (this.ShouldStandardFieldBeSkipped(field))
      {
        result = false;
      }
      #region Fix TFS #325588
      //else if (StandardValuesManager.IsStandardValuesHolder(item))
      //{
      //    result = false;
      //}
      #endregion
      else if (field.ID == FieldIDs.EnableLanguageFallback || field.ID == FieldIDs.EnableSharedLanguageFallback)
      {
        result = false;
      }
      else if (!field.SharedLanguageFallbackEnabled)
      {
        if (Settings.LanguageFallback.AllowVaryFallbackSettingsPerLanguage)
        {
          Item innerItem;
          using (new LanguageFallbackItemSwitcher(false))
          {
            innerItem = field.InnerItem;
          }

          if (innerItem == null || innerItem.Fields[FieldIDs.EnableLanguageFallback].GetValue(false, false) != "1")
          {
            result = false;
          }
        }
        else
        {
          #region Fix TFS #105327
          Item innerItem2;
          using (new LanguageFallbackItemSwitcher(new bool?(false)))
          {
            innerItem2 = field.InnerItem;
          }
          if (innerItem2 == null || innerItem2.Fields[FieldIDs.EnableSharedLanguageFallback].GetValue(true, false) != "1")
          {
            result = false;
          }
          #endregion
        }
      }

      this.AddFallbackIsValidValueToCache(field, key, result);
      return result;
    }
  }
}
