using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using GlazeWM.Domain.Common.Events;
using GlazeWM.Domain.Containers;
using GlazeWM.Domain.UserConfigs;
using GlazeWM.Infrastructure;
using GlazeWM.Infrastructure.Bussing;

namespace GlazeWM.Bar.Components
{
  public class BindingModeComponentViewModel : ComponentViewModel
  {
    private readonly BindingModeComponentConfig _config;
    private readonly Bus _bus = ServiceLocator.GetRequiredService<Bus>();
    private readonly ContainerService _containerService =
      ServiceLocator.GetRequiredService<ContainerService>();

    /// <summary>
    /// Name of the currently active binding mode (if one is active).
    /// </summary>
    public string ActiveBindingMode => _containerService.ActiveBindingMode;

    /// <summary>
    /// Hide component when no binding mode is active.
    /// </summary>
    public override string Visibility =>
      _config.DefaultLabel is "" ? "Collapsed" : "Visible"; //Uses the config value as the deciding factor on visibility, seems to work

    private LabelViewModel _label;
    public LabelViewModel Label
    {
      get => _label;
      protected set => SetField(ref _label, value);
    }

    public BindingModeComponentViewModel(
      BarViewModel parentViewModel,
      BindingModeComponentConfig config) : base(parentViewModel, config)
    {
      _config = config;
      Label = CreateLabel(_config.DefaultLabel); //Propagates the default tag on startup

      _bus.Events.OfType<BindingModeChangedEvent>()
        .TakeUntil(_parentViewModel.WindowClosing)
        .Subscribe((@event) =>
        {
          OnPropertyChanged(nameof(Visibility));
          Label = ActiveBindingMode is null ? CreateLabel(_config.DefaultLabel) : CreateLabel(@event.NewBindingMode); //Ensures that Label reverts to default tag once binding mode is exited.
        });
    }

    private LabelViewModel CreateLabel(string bindingMode)
    {
      var variableDictionary = new Dictionary<string, Func<string>>()
      {
        { "binding_mode", () => bindingMode }
      };

      return XamlHelper.ParseLabel(_config.Label, variableDictionary, this);
    }
  }
}
