using ComicsUniverse.Contracts.Services;
using ComicsUniverse.Contracts.ViewModels;
using ComicsUniverse.Core.Contracts.Services;
using ComicsUniverse.Core.Dtos;
using ComicsUniverse.Views;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace ComicsUniverse.ViewModels
{
    public class CharactersViewModel : ObservableRecipient, INavigationAware
    {
        private readonly ICharacterService _characterService;
        private readonly INavigationService _navigationService;

        public CharactersViewModel(ICharacterService characterService, INavigationService navigationService)
        {
            _characterService = characterService;
            _navigationService = navigationService;
        }

        private CharacterViewModel _selectedCharacter;
        public CharacterViewModel Selected
        {
            get => _selectedCharacter;
            set => SetProperty(ref _selectedCharacter, value);
        }

        private ICommand _deleteCommand;
        public ICommand DeleteCommand
        {
            get
            {
                if (_deleteCommand == null)
                {
                    _deleteCommand = new RelayCommand<CharacterViewModel>(async param =>
                    {
                        ContentDialog deleteDialog = new()
                        {
                            Title = "Delete character permanently?",
                            Content = "If you delete this character, you won't be able to recover it. Do you want to delete it?",
                            PrimaryButtonText = "Delete",
                            CloseButtonText = "Cancel",
                            DefaultButton = ContentDialogButton.Close,
                            XamlRoot = _navigationService.Frame.XamlRoot
                        };

                        ContentDialogResult result = await deleteDialog.ShowAsync();

                        if (result == ContentDialogResult.Primary)
                        {
                            if (await _characterService.DeleteCharacterAsync((CharacterDto)param))
                            {
                                _ = Characters.Remove(param);
                            }
                        }
                    }, param => param != null);
                }

                return _deleteCommand;
            }
        }

        private ICommand _addCommand;
        public ICommand AddCommand
        {
            get
            {
                if (_addCommand == null)
                {
                    _addCommand = new RelayCommand(async () =>
                    {
                        CharacterViewModel newCharacter = new() { ProfileImage = "Unknown.jpg" };
                        CharacterPage page = new(newCharacter);

                        ContentDialog dialog = new()
                        {
                            Title = "Add new character",
                            Content = page,
                            PrimaryButtonText = "Add",
                            IsPrimaryButtonEnabled = false,
                            CloseButtonText = "Cancel",
                            DefaultButton = ContentDialogButton.Primary,
                            XamlRoot = _navigationService.Frame.XamlRoot
                        };

                        newCharacter.PropertyChanged += (sender, e) => dialog.IsPrimaryButtonEnabled = !newCharacter.HasErrors;

                        ContentDialogResult result = await dialog.ShowAsync();

                        if (result == ContentDialogResult.Primary)
                        {
                            var characterDto = await _characterService.CreateCharacterAsync((CharacterDto)newCharacter);
                            CharacterViewModel character = new(characterDto);

                            Characters.Add(character);
                            Selected = character;
                        }
                    });
                }

                return _addCommand;
            }
        }

        private ICommand _updateCommand;
        public ICommand UpdateCommand
        {
            get
            {
                // if private ICommand is null
                if (_updateCommand == null)
                {
                    // 
                    _updateCommand = new RelayCommand<CharacterViewModel>(async param =>
                    {
                        CharacterViewModel newCharacter = new() { ProfileImage = "Unknown.jpg" };
                        CharacterPage page = new(newCharacter);

                        ContentDialog updateDialog = new()
                        {
                            Title = "Update character",
                            Content = page,
                            PrimaryButtonText = "Update",
                            IsPrimaryButtonEnabled = false,
                            CloseButtonText = "Cancel",

                            DefaultButton = ContentDialogButton.Close,
                            XamlRoot = _navigationService.Frame.XamlRoot
                        };
                        // enables the primaryButton (UpdateButton) if the newCharacter doesn't have errors or missing required info
                        newCharacter.PropertyChanged += (sender, e) => updateDialog.IsPrimaryButtonEnabled = !newCharacter.HasErrors;

                        // contentDialogResult is an enum. result will be primary (1) if user clicks the primaryButton (UpdateButton)
                        ContentDialogResult result = await updateDialog.ShowAsync();

                        // if the primary button in contentdialog is tapped
                        if (result == ContentDialogResult.Primary)
                        {
                            // returns true if    , returns false if not
                            if (await _characterService.DeleteCharacterAsync((CharacterDto)param))
                            {
                                _ = Characters.Remove(param);
                            }
                            Characters.Add(param);
                        }
                    }, param => param != null);
                }

                return _updateCommand;
            }
        }

        private ICommand _settingsCommand;
        public ICommand SettingsCommand
        {
            get
            {
                if (_settingsCommand == null)
                {
                    _settingsCommand = new RelayCommand(() =>
                    {
                        _navigationService.NavigateTo(typeof(SettingsViewModel).FullName);
                    });
                }

                return _settingsCommand;
            }
        }

        public ObservableCollection<CharacterViewModel> Characters { get; private set; } = new();

        public async void OnNavigatedTo(object parameter)
        {
            if (Characters.Count == 0)
            {
                var characterDtos = await _characterService.GetCharactersAsync();

                foreach (var characterDto in characterDtos)
                {
                    Characters.Add(new CharacterViewModel(characterDto));
                }
            }
        }

        public void OnNavigatedFrom() { }

        public void EnsureItemSelected()
        {
            if (Selected == null && Characters.Count > 0)
            {
                Selected = Characters.First();
            }
        }
    }
}
