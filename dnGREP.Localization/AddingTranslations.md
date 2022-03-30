## Adding Translations to dnGrep

1. New language resources (*.resx) are created in [Weblate](https://hosted.weblate.org/projects/dngrep/dngrep-application/). It seems to create less merge problems if they are created from Weblate rather than from the application. 
2. From the Weblate Repository Maintenance page, commit any pending changes and push the commits to GitHub. Commits will be pushed to the "weblate-translate" branch.
2. In GitHub, create a pull request from the weblate-translate branch, and merge into master.
3. Pull the master branch into your local repo.
4. Create a new local branch from master for the changes to include the new language.
5. In the dnGREP.Localization project, include the new Resources.xx.resx file into the project.
6. In the TranslationSource.cs file, add the language tag and language name to the AppCultures dictionary. The language name should be in the native language.
7. In the dnGREP.Setup project, edit the AppFragments.wxs file to add the language-specific `<Directory>` to the setup project. Create new Guids for the Directory Id, Component Id, Component Guid, and File Id, following the existing pattern. Add the new Component Id to the `<ComponentGroup>` in a new `<ComponentRef>`.
8. In the TestLocalizedStrings project, edit the TestStringsViewModel.cs file to add the language tag and language name to the AppCultures dictionary.
9. Rebuild the application, and run the TestLocalizedString project. Select the new language, and check for errors in the string substitutions.
10. Run the dnGrep application, change to the new language and check for layout problems.
11. Open the dnGrep project on [SignPath](https://app.signpath.io/Web/736ab30b-dc3e-41ee-800d-c5674d702ed8/Projects/d37d257d-e767-4d90-9fd9-e127dfd82f1d). Open the 'Default' Artifact Configuration, and add click the 'Edit' button. Add a new include path for the new language, like this: `<include path="es" />`
12. Commit the changes, and create a pull request for the new language resource. Merge to master.
 