using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class Phase40VisualDescriptorTests
{
    [Fact]
    public void Catalog_ContainsExactlyThePhase40Visuals()
    {
        Assert.Equal(
            ["card", "table", "clusteredColumnChart", "lineChart", "barChart", "pieChart"],
            Phase40VisualDescriptorCatalog.All.Select(descriptor => descriptor.VisualType));
    }

    [Theory]
    [InlineData("card", "Fields", "Value")]
    [InlineData("table", "Values", "Value")]
    [InlineData("clusteredColumnChart", "Category", "Category")]
    [InlineData("lineChart", "Series", "Series")]
    [InlineData("barChart", "Y", "Value")]
    [InlineData("pieChart", "Category", "Legend")]
    public void Catalog_ExposesTypedRoleProjection(string visualType, string serializerRole, string bindingRole)
    {
        var descriptor = Phase40VisualDescriptorCatalog.Get(visualType);

        Assert.Contains(descriptor.SerializerRoles, role => role.SerializerRole == serializerRole && role.BindingRole.ToString() == bindingRole);
    }

    [Theory]
    [InlineData("card", "Fields", "Value")]
    [InlineData("table", "Values", "Value")]
    [InlineData("clusteredColumnChart", "Category", "Category")]
    [InlineData("lineChart", "Series", "Series")]
    [InlineData("barChart", "Y", "Value")]
    [InlineData("pieChart", "Category", "Legend")]
    [InlineData("slicer", "Category", "Category")]
    public void Catalog_ResolvesImportedRoleAliasesToTheCanonicalSharedRole(string visualType, string importedRole, string bindingRole)
    {
        var projection = Assert.Single(Phase40VisualDescriptorCatalog.ResolveImportedRoles(visualType, importedRole));

        Assert.Equal(bindingRole, projection.BindingRole.ToString());
    }

    [Fact]
    public void Catalog_ResolvesSerializerTooltipRoleToTheSharedTooltipRole()
    {
        var projection = Assert.Single(Phase40VisualDescriptorCatalog.ResolveImportedRoles("lineChart", "Tooltips"));

        Assert.Equal(LocalPbirGenerationBindingRole.Tooltip, projection.BindingRole);
    }

    [Fact]
    public void TemplateCatalog_UsesStableStronglyTypedDefaults()
    {
        Assert.Equal(LocalPbirGenerationVisualTemplate.Default, Phase40VisualTemplateCatalog.Get("default").Name);
        Assert.Equal(LocalPbirGenerationVisualTemplate.Executive, Phase40VisualTemplateCatalog.Get("executive").Name);
        Assert.Equal(LocalPbirGenerationVisualTemplate.Compact, Phase40VisualTemplateCatalog.Get("compact").Name);
        Assert.Equal("#FFFFFF", Phase40VisualTemplateCatalog.Get("default").Chart.Background!.Hex);
    }
}
