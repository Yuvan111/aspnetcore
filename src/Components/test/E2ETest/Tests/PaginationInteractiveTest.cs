// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.Tests;

public class PaginationInteractiveTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public PaginationInteractiveTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync() => InitializeAsync(BrowserFixture.StreamingContext);

    [Fact]
    public void SimplePaginationButtonsNavigateBetweenPages()
    {
        Navigate($"{ServerPathBase}/pagination-interactive");
        Browser.Exists(By.CssSelector("section.sample-block:nth-of-type(1) table.quickgrid"));

        var pageOneRowCount = Browser.FindElement(By.CssSelector("section.sample-block:nth-of-type(1) table.quickgrid tbody")).FindElements(By.CssSelector("tr")).Count;
        Assert.Equal(5, pageOneRowCount);

        Browser.Equal("1", () => Browser.FindElement(By.CssSelector("section.sample-block:nth-of-type(1) .paginator .pagination-text strong:first-child")).Text);
        Browser.Equal("2", () => Browser.FindElement(By.CssSelector("section.sample-block:nth-of-type(1) .paginator .pagination-text strong:last-child")).Text);

        Browser.Click(By.CssSelector("section.sample-block:nth-of-type(1) .go-next"));

        Browser.Equal("2", () => Browser.FindElement(By.CssSelector("section.sample-block:nth-of-type(1) .paginator .pagination-text strong:first-child")).Text);
        Browser.Equal("12130", () => Browser.FindElement(By.CssSelector("section.sample-block:nth-of-type(1) table.quickgrid tbody tr:nth-child(1) td:nth-child(1)")).Text);

        Browser.Click(By.CssSelector("section.sample-block:nth-of-type(1) .go-previous"));

        Browser.Equal("1", () => Browser.FindElement(By.CssSelector("section.sample-block:nth-of-type(1) .paginator .pagination-text strong:first-child")).Text);
        Browser.Equal("11203", () => Browser.FindElement(By.CssSelector("section.sample-block:nth-of-type(1) table.quickgrid tbody tr:nth-child(1) td:nth-child(1)")).Text);
    }

    [Fact]
    public void SummaryTemplateUpdatesWhenNavigatingPages()
    {
        Navigate($"{ServerPathBase}/pagination-interactive");
        Browser.Exists(By.CssSelector("section.sample-block:nth-of-type(2) .paginator"));

        Browser.Equal("Showing 1 of 2 pages", () => Browser.FindElement(By.CssSelector("section.sample-block:nth-of-type(2) .summary")).Text.Trim());

        Browser.Click(By.CssSelector("section.sample-block:nth-of-type(2) .go-next"));

        Browser.Equal("Showing 2 of 2 pages", () => Browser.FindElement(By.CssSelector("section.sample-block:nth-of-type(2) .summary")).Text.Trim());
    }
}

