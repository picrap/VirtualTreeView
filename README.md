<table width="100%" style="border: 0 !important">
<tr>
<td rowspan=2>
<img src="https://raw.githubusercontent.com/picrap/VirtualTreeView/master/Icon/VirtualTreeView.png" width=128 height=128 /></td>
<td><h1>VirtualTreeView</h1></td>
</tr>
<tr><td>A WPF TreeView that actually supports virtualization.<br/>
Works with .NET Framework 4.5.  </td></tr>
</table>

## How to use it

It is available as a [NuGet package](https://www.nuget.org/packages/VirtualTreeView/).  
The source code includes a demonstration application, where both modes (content and binding) are showed, side-by-side to traditional `TreeView` control.

### The treeview itself

It works exactly as the original `TreeView` (with a lot of missing features...):

```xaml
<vtv:VirtualTreeView ItemsSource="{Binding Items}">
    <!-- Item style, if there is only one, can also be set using the ItemStyle property -->
    <vtv:VirtualTreeView.Resources>
        <!-- The important stuff here is the ItemsSource -->
        <HierarchicalDataTemplate ItemsSource="{Binding Children}" 
                                  DataType="{x:Type my:ViewModels}">
            <Grid>
                <TextBlock Text="{Binding MyLabel}"/>
            </Grid>
        </HierarchicalDataTemplate>
    </vtv:VirtualTreeView.Resources>
    <!-- This is optional if you want to bind the IsExpanded property -->
    <vtv:VirtualTreeView.ItemContainerStyle>
        <Style TargetType="{x:Type vtv:VirtualTreeViewItem}">
            <Setter Property="IsExpanded" Value="{Binding MyIsExpanded, Mode=TwoWay}" />
        </Style>
    </vtv:VirtualTreeView.ItemContainerStyle>
</vtv:VirtualTreeView>
```

### The treeview item

If you move from an existing treeView, you'll need to copy/paste your `TreeViewItem` style and adapt it to `vtv:TreeViewItem` (usually no change is required, except removing the hierarchical part, which is generated when the control converts the tree to a list).

### What does work

Currently it has only the features I needed (which is showing items and let them live); all contributors are welcome. The goal is to have a complete and extensible tree view.  
* With binding, `INotifyCollectionChanged` fully works, so you can dynamically change content by adding or removing elements at any point of the hierarchy.

### What does not

* With binding, `INotifyPropertyChange` **does not work at all**, since the binding is partly simulated (because the view items are not generated).


### How to contribute

[Fork it](https://github.com/picrap/VirtualTreeView#fork-destination-box), update it, and submit your pull requests.  
Alternatively you can [submit requests](https://github.com/picrap/VirtualTreeView/issues).  
