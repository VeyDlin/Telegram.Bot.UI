using FluentAssertions;
using Telegram.Bot.UI.Parsing;

namespace Telegram.Bot.UI.Tests.Unit;

public class SelfClosingTagPreprocessorTests {
    [Fact]
    public void SimpleTag_ExpandsToOpenClose() {
        var result = SelfClosingTagPreprocessor.Process("<command />");
        result.Should().Be("<command ></command>");
    }

    [Fact]
    public void TagWithSingleAttribute_Preserved() {
        var result = SelfClosingTagPreprocessor.Process("""<command title="Save" />""");
        result.Should().Be("""<command title="Save"></command>""");
    }

    [Fact]
    public void TagWithMultipleAttributes_AllPreserved() {
        var result = SelfClosingTagPreprocessor.Process("""<open id="pg" title="Go" target="other" columns="2" />""");
        result.Should().Be("""<open id="pg" title="Go" target="other" columns="2"></open>""");
    }

    [Fact]
    public void HyphenatedTagName_Supported() {
        var result = SelfClosingTagPreprocessor.Process("""<checkbox-list id="cl" />""");
        result.Should().Be("""<checkbox-list id="cl"></checkbox-list>""");
    }

    [Fact]
    public void DoubleHyphenatedTagName_Supported() {
        var result = SelfClosingTagPreprocessor.Process("""<my-custom-tag id="x" />""");
        result.Should().Be("""<my-custom-tag id="x"></my-custom-tag>""");
    }

    [Fact]
    public void VueBindShorthand_PreservedInAttributes() {
        var result = SelfClosingTagPreprocessor.Process("""<option :value="item.id" :title="item.name" />""");
        result.Should().Be("""<option :value="item.id" :title="item.name"></option>""");
    }

    [Fact]
    public void VueEventShorthand_PreservedInAttributes() {
        var result = SelfClosingTagPreprocessor.Process("""<command title="Go" @click="doStuff()" />""");
        result.Should().Be("""<command title="Go" @click="doStuff()"></command>""");
    }

    [Fact]
    public void VForDirective_PreservedInAttributes() {
        var result = SelfClosingTagPreprocessor.Process("""<option v-for="v in items" :value="v.id" :title="v.name" />""");
        result.Should().Be("""<option v-for="v in items" :value="v.id" :title="v.name"></option>""");
    }

    [Fact]
    public void VIfDirective_PreservedInAttributes() {
        var result = SelfClosingTagPreprocessor.Process("""<command v-if="showBtn" title="Conditional" />""");
        result.Should().Be("""<command v-if="showBtn" title="Conditional"></command>""");
    }

    [Fact]
    public void QuotedSlashInsideAttribute_NotConfused() {
        var result = SelfClosingTagPreprocessor.Process("""<command title="path/to/thing" />""");
        result.Should().Be("""<command title="path/to/thing"></command>""");
    }

    [Fact]
    public void QuotedGreaterThanInsideAttribute_NotConfused() {
        var result = SelfClosingTagPreprocessor.Process("""<command title="a > b" />""");
        result.Should().Be("""<command title="a > b"></command>""");
    }

    [Fact]
    public void SingleQuotedAttribute_Handled() {
        var result = SelfClosingTagPreprocessor.Process("<option value='test' title='Hello' />");
        result.Should().Be("<option value='test' title='Hello'></option>");
    }

    [Fact]
    public void MixedQuotes_Handled() {
        var result = SelfClosingTagPreprocessor.Process("""<command title="it's" id='x"y' />""");
        result.Should().Be("""<command title="it's" id='x"y'></command>""");
    }

    [Fact]
    public void NoSpaceBeforeSlash_StillExpands() {
        var result = SelfClosingTagPreprocessor.Process("""<command title="X"/>""");
        result.Should().Be("""<command title="X"></command>""");
    }

    [Fact]
    public void ExtraSpacesAroundSlash_Handled() {
        var result = SelfClosingTagPreprocessor.Process("""<command title="X"  /  >""");
        result.Should().Be("""<command title="X"></command>""");
    }

    [Fact]
    public void NoAttributes_JustSelfClose() {
        var result = SelfClosingTagPreprocessor.Process("<navigate />");
        result.Should().Be("<navigate ></navigate>");
    }

    [Fact]
    public void MultipleSelfClosingTags_AllExpanded() {
        var input = """
            <command title="A" />
            <command title="B" />
            <option value="1" />
            """;
        var result = SelfClosingTagPreprocessor.Process(input);

        result.Should().Contain("""<command title="A"></command>""");
        result.Should().Contain("""<command title="B"></command>""");
        result.Should().Contain("""<option value="1"></option>""");
    }

    [Fact]
    public void MixedSelfClosingAndNormal_OnlySelfClosingExpanded() {
        var input = """
            <radio id="r1">
                <option value="a" title="A" />
                <option value="b" title="B" />
            </radio>
            """;
        var result = SelfClosingTagPreprocessor.Process(input);

        result.Should().Contain("<radio id=\"r1\">");
        result.Should().Contain("""<option value="a" title="A"></option>""");
        result.Should().Contain("""<option value="b" title="B"></option>""");
        result.Should().Contain("</radio>");
    }

    [Fact]
    public void VoidElement_Br_SelfClosing_Untouched() {
        SelfClosingTagPreprocessor.Process("<br />").Should().Be("<br />");
    }

    [Fact]
    public void VoidElement_Br_NoSlash_Untouched() {
        SelfClosingTagPreprocessor.Process("<br>").Should().Be("<br>");
    }

    [Fact]
    public void VoidElement_Br_NoSpace_Untouched() {
        SelfClosingTagPreprocessor.Process("<br/>").Should().Be("<br/>");
    }

    [Fact]
    public void VoidElement_Hr_SelfClosing_Untouched() {
        SelfClosingTagPreprocessor.Process("<hr />").Should().Be("<hr />");
    }

    [Fact]
    public void VoidElement_Hr_NoSlash_Untouched() {
        SelfClosingTagPreprocessor.Process("<hr>").Should().Be("<hr>");
    }

    [Fact]
    public void VoidElement_Img_SelfClosing_Untouched() {
        var input = """<img src="photo.jpg" alt="pic" />""";
        SelfClosingTagPreprocessor.Process(input).Should().Be(input);
    }

    [Fact]
    public void VoidElement_Img_NoSlash_Untouched() {
        var input = """<img src="photo.jpg" alt="pic">""";
        SelfClosingTagPreprocessor.Process(input).Should().Be(input);
    }

    [Fact]
    public void VoidElement_Input_SelfClosing_Untouched() {
        var input = """<input type="text" name="q" />""";
        SelfClosingTagPreprocessor.Process(input).Should().Be(input);
    }

    [Fact]
    public void VoidElement_Meta_SelfClosing_Untouched() {
        var input = """<meta charset="utf-8" />""";
        SelfClosingTagPreprocessor.Process(input).Should().Be(input);
    }

    [Fact]
    public void VoidElement_Link_SelfClosing_Untouched() {
        var input = """<link rel="stylesheet" href="style.css" />""";
        SelfClosingTagPreprocessor.Process(input).Should().Be(input);
    }

    [Fact]
    public void VoidElement_Col_SelfClosing_Untouched() {
        SelfClosingTagPreprocessor.Process("""<col span="2" />""").Should().Be("""<col span="2" />""");
    }

    [Fact]
    public void VoidElement_Embed_SelfClosing_Untouched() {
        SelfClosingTagPreprocessor.Process("""<embed src="x.swf" />""").Should().Be("""<embed src="x.swf" />""");
    }

    [Fact]
    public void VoidElement_Source_SelfClosing_Untouched() {
        SelfClosingTagPreprocessor.Process("""<source src="a.mp3" />""").Should().Be("""<source src="a.mp3" />""");
    }

    [Fact]
    public void VoidElement_Track_SelfClosing_Untouched() {
        SelfClosingTagPreprocessor.Process("""<track src="sub.vtt" />""").Should().Be("""<track src="sub.vtt" />""");
    }

    [Fact]
    public void VoidElement_Wbr_SelfClosing_Untouched() {
        SelfClosingTagPreprocessor.Process("<wbr />").Should().Be("<wbr />");
    }

    [Fact]
    public void VoidElement_Area_SelfClosing_Untouched() {
        SelfClosingTagPreprocessor.Process("""<area shape="rect" />""").Should().Be("""<area shape="rect" />""");
    }

    [Fact]
    public void VoidElement_Base_SelfClosing_Untouched() {
        SelfClosingTagPreprocessor.Process("""<base href="/" />""").Should().Be("""<base href="/" />""");
    }

    [Fact]
    public void VoidElementAmongCustomTags_OnlyCustomExpanded() {
        var input = """
            <message>
                Hello<br/>World<br />!
            </message>
            <components>
                <command title="OK" />
            </components>
            """;
        var result = SelfClosingTagPreprocessor.Process(input);

        result.Should().Contain("<br/>");
        result.Should().Contain("<br />");
        result.Should().Contain("""<command title="OK"></command>""");
    }

    [Fact]
    public void VoidElement_CaseInsensitive_Untouched() {
        SelfClosingTagPreprocessor.Process("<BR />").Should().Be("<BR />");
        SelfClosingTagPreprocessor.Process("<Br />").Should().Be("<Br />");
        SelfClosingTagPreprocessor.Process("<Hr />").Should().Be("<Hr />");
        SelfClosingTagPreprocessor.Process("<IMG src=\"x.png\" />").Should().Be("<IMG src=\"x.png\" />");
    }

    [Fact]
    public void FullPage_VoidAndCustomMixed_CorrectResult() {
        var input = """
            <view id="demo">
                <title>Test</title>
                <message>
                    Line 1<br/>Line 2<br />Line 3
                    <hr />
                </message>
                <components>
                    <command title="Save" variant="success" />
                    <checkbox id="cb" title="Check" />
                </components>
            </view>
            """;
        var result = SelfClosingTagPreprocessor.Process(input);

        result.Should().Contain("<br/>");
        result.Should().Contain("<br />");
        result.Should().Contain("<hr />");
        result.Should().Contain("""<command title="Save" variant="success"></command>""");
        result.Should().Contain("""<checkbox id="cb" title="Check"></checkbox>""");
    }

    [Fact]
    public void FullPageMarkup_ExpandsAllSelfClosing() {
        var input = """
            <view id="test">
                <title>Test</title>
                <message>Hello</message>
                <components>
                    <command id="btn1" title="Click" variant="primary" />
                    <row>
                        <open id="pg" title="Go" target="other" columns="2" />
                        <command id="btn2" title="Delete" variant="danger" columns="2" />
                    </row>
                    <radio id="r" columns="1">
                        <option v-for="x in list" :value="x.id" :title="x.name" />
                    </radio>
                    <navigate prevTitle="Prev" nextTitle="Next" />
                </components>
            </view>
            """;
        var result = SelfClosingTagPreprocessor.Process(input);

        result.Should().Contain("""<command id="btn1" title="Click" variant="primary"></command>""");
        result.Should().Contain("""<open id="pg" title="Go" target="other" columns="2"></open>""");
        result.Should().Contain("""<command id="btn2" title="Delete" variant="danger" columns="2"></command>""");
        result.Should().Contain("""<option v-for="x in list" :value="x.id" :title="x.name"></option>""");
        result.Should().Contain("""<navigate prevTitle="Prev" nextTitle="Next"></navigate>""");
        result.Should().NotContain("/>");
    }

    [Fact]
    public void VariantAndIcon_PreservedInAttributes() {
        var result = SelfClosingTagPreprocessor.Process("""<command title="Save" variant="success" icon="5368324170671202286" />""");
        result.Should().Be("""<command title="Save" variant="success" icon="5368324170671202286"></command>""");
    }

    [Fact]
    public void MustacheInAttribute_PreservedCorrectly() {
        var result = SelfClosingTagPreprocessor.Process("""<command title="{{ count + ' items' }}" />""");
        result.Should().Be("""<command title="{{ count + ' items' }}"></command>""");
    }

    [Fact]
    public void EmptyStringAttribute_Preserved() {
        var result = SelfClosingTagPreprocessor.Process("""<command title="" id="x" />""");
        result.Should().Be("""<command title="" id="x"></command>""");
    }

    [Fact]
    public void TextBetweenTags_Untouched() {
        var input = "<message>Some text with / and > inside</message>";
        var result = SelfClosingTagPreprocessor.Process(input);
        result.Should().Be(input);
    }

    [Fact]
    public void ScriptBlock_Untouched() {
        var input = """
            <script>
                var x = 1 / 2;
                if (x > 0) { doStuff(); }
            </script>
            """;
        var result = SelfClosingTagPreprocessor.Process(input);
        result.Should().Be(input);
    }

    [Fact]
    public void MultilineAttributes_Handled() {
        var input = """
            <command
                id="btn"
                title="Save"
                variant="primary"
            />
            """;
        var result = SelfClosingTagPreprocessor.Process(input);

        result.Should().Contain("</command>");
        result.Should().NotContain("/>");
    }

    // --- Explicit close tags (should remain untouched) ---

    [Fact]
    public void ExplicitCloseTag_EmptyBody_Untouched() {
        var input = """<command title="Save"></command>""";
        var result = SelfClosingTagPreprocessor.Process(input);
        result.Should().Be(input);
    }

    [Fact]
    public void ExplicitCloseTag_WithChildren_Untouched() {
        var input = """
            <radio id="r1" columns="1">
                <option value="a" title="A"></option>
                <option value="b" title="B"></option>
            </radio>
            """;
        var result = SelfClosingTagPreprocessor.Process(input);
        result.Should().Be(input);
    }

    [Fact]
    public void ExplicitCloseTag_ContainerElements_Untouched() {
        var input = """
            <view id="test">
                <title>Hello</title>
                <message>World</message>
                <components>
                    <command title="OK"></command>
                </components>
            </view>
            """;
        var result = SelfClosingTagPreprocessor.Process(input);
        result.Should().Be(input);
    }

    [Fact]
    public void ExplicitCloseTag_SwitchWithOptions_Untouched() {
        var input = """
            <switch id="sw" title="Mode">
                <option value="on" title="On"></option>
                <option value="off" title="Off"></option>
            </switch>
            """;
        var result = SelfClosingTagPreprocessor.Process(input);
        result.Should().Be(input);
    }

    [Fact]
    public void ExplicitCloseTag_RowWithButtons_Untouched() {
        var input = """
            <row>
                <command title="A" columns="2"></command>
                <command title="B" columns="2"></command>
            </row>
            """;
        var result = SelfClosingTagPreprocessor.Process(input);
        result.Should().Be(input);
    }

    [Fact]
    public void MixedExplicitAndSelfClosing_OnlySelfClosingExpanded() {
        var input = """
            <switch id="sw" title="Mode">
                <option value="on" title="On" />
                <option value="off" title="Off"></option>
            </switch>
            """;
        var result = SelfClosingTagPreprocessor.Process(input);

        result.Should().Contain("""<option value="on" title="On"></option>""");
        result.Should().Contain("""<option value="off" title="Off"></option>""");
        result.Should().Contain("<switch id=\"sw\" title=\"Mode\">");
        result.Should().Contain("</switch>");
    }

    [Fact]
    public void ExplicitCloseTag_CheckboxListWithOptions_Untouched() {
        var input = """
            <checkbox-list id="cl" columns="1">
                <option value="1" title="One"></option>
                <option value="2" title="Two"></option>
            </checkbox-list>
            """;
        var result = SelfClosingTagPreprocessor.Process(input);
        result.Should().Be(input);
    }

    [Fact]
    public void ExplicitCloseTag_NavigateWithAttributes_Untouched() {
        var input = """<navigate prevTitle="Back" nextTitle="Next"></navigate>""";
        var result = SelfClosingTagPreprocessor.Process(input);
        result.Should().Be(input);
    }

    [Fact]
    public void FullPage_MixedStyles_CorrectResult() {
        var input = """
            <view id="mix">
                <title>Mixed</title>
                <message>Test</message>
                <components>
                    <checkbox id="cb" title="Check" />
                    <radio id="r" columns="1">
                        <option value="a" title="A" />
                        <option value="b" title="B"></option>
                    </radio>
                    <row>
                        <open id="p" title="Open" target="page" columns="2" />
                        <command title="Act" columns="2"></command>
                    </row>
                    <navigate />
                </components>
            </view>
            """;
        var result = SelfClosingTagPreprocessor.Process(input);

        result.Should().Contain("""<checkbox id="cb" title="Check"></checkbox>""");
        result.Should().Contain("""<option value="a" title="A"></option>""");
        result.Should().Contain("""<option value="b" title="B"></option>""");
        result.Should().Contain("""<open id="p" title="Open" target="page" columns="2"></open>""");
        result.Should().Contain("""<command title="Act" columns="2"></command>""");
        result.Should().Contain("<navigate ></navigate>");
        result.Should().NotContain("/>");
    }
}
