var isConversationStarted = false;
var isMaximized = false;
var output = '';
var DirectLineSecret = '';

var colorArr = ['color1', 'color2', 'color3', 'color4', 'color5','color6', 'color7', 'color8', 'color9', 'color10','color11', 'color12', 'color13', 'color14', 'color15'];
var ConversationID = '';
var Token;
var DataID;
var Watermark = -1;
var CardOriginalHTML = '<div class="dvCard">' +
    '<div class="dvCardTitle" style="font-weight:bold;">@</div>' +
    '<div class="dvCardText">#</div>' +
    '<div class="dvCardButtons">$</div>' +
'</div>'
var CardHTML = CardOriginalHTML;

$('.ChatMain').css({ 'display': 'none' });

$('.btnMinimize').click(function () {
    if ($('.ChatMain').css('display') == 'none') {
        $('.ChatMain').css({ 'display': 'block' });
        $('.ChatButton').css({ 'display': 'none' });
        isMaximized = true;
        //$('.ChatHeader > div').removeClass('dropup').addClass('dropdown');
    }
    else {
        $('.ChatMain').css({ 'display': 'none' });
        $('.ChatButton').css({ 'display': 'block' });
        isMaximized = false;
        //$('.ChatHeader > div').removeClass('dropdown').addClass('dropup');
    }
});

$('.ChatButton').click(function () {
    $('.ChatMain').css({ 'display': 'block' });
    $('.ChatButton').css({ 'display': 'none' });
    if (!isConversationStarted) {
        fnStartConversation();
    }
});

//$('#lnkStartEndConv').click(function () {
//    if (!isConversationStarted) {
//        fnStartConversation();
//    }
//    else {
//        fnEndConversation();
//    }
//});

$('#btnSend').click(function (event) {
    if ($.trim($('#txtInput').val()) != '' && isConversationStarted) {
        fnSendReceiveCall($('#txtInput').val());
    }
    else {
        alert('Start the conversation first');
    }
});

function fnScrollDown() {
    var height = $('.ChatArea')[0].scrollHeight;
    $('.ChatArea').scrollTop(height);
}

$('#lnkEndCov').click(function () {
    if (isConversationStarted) {       
        fnEndConversation();
    }
    $('.ChatArea').empty();
    $('.ChatMain').css({ 'display': 'none' });
    $('.ChatButton').css({ 'display': 'block' });
    isMaximized = false;
    //$('.ChatArea').find('*').not('.alert-msg,.alert-success').remove();
});

$(document).on('click', 'button[class^="btnCard"]', function (event) {
    fnSendReceiveCall($(this).text());
});

$('#txtInput').keypress(function (e) {
    if (e.which == 13) {//Enter key pressed
        $('#btnSend').click();//Trigger search button click event
        return false;
    }
});

function fnSendReceiveCall(input) {
    var ReceiveHandle;
    $('#txtInput').val('');
    if (ConversationID != '') {
        var SendHandle = fnSendMessage(ConversationID, input);
        fnScrollDown();
        SendHandle.done(function () {            
            ReceiveHandle = fnReceiveMessage(ConversationID);
            fnScrollDown();
            ReceiveHandle.done(function () {
                fnScrollDown();
            });
        });
    }
    else {
        alert('Start the conversation first');
    }
}

function addChat(type, content) {
    var innerHTML;
    var timeStamp;
    var ReceivingMsg;

    if (type != 'Spinner') {
        innerHTML = '<div class="' + (type == 'User' ? 'usr' : 'bot') + '">' + content + '</div>';
        timeStamp = '<div class="TimeStamp">' + formatAMPM(type) + '</div>';
        innerHTML += timeStamp;
    }
    else {
        ReceivingMsg = '<div class="TimeStamp">Receiving...</div>';
        innerHTML = content + ReceivingMsg;
    }

    $(innerHTML)
        .hide()
        .appendTo('.ChatArea')
        .fadeIn(1000,'linear',undefined);
}

function getUserFormattedInput(input) {
    //return '<span style="white-space:pre-wrap">' + $.trim(input) + '</span>';
    return $.trim(input);
}

function formatAMPM(type) {
    var date = new Date();
    var hours = date.getHours();
    var minutes = date.getMinutes();
    var ampm = hours >= 12 ? ' PM' : ' AM';
    hours = hours % 12;
    hours = hours ? hours : 12; // the hour '0' should be '12'
    minutes = minutes < 10 ? '0' + minutes : minutes;
    var strTime = type + ' at ' + hours + ':' + minutes + ' ' + ampm;
    return strTime;
}

function fnToggleElement(flag) {
    if (flag) {
        $('#lnkStartEndConv').css('color', 'limegreen');
        $('#lnkStartEndConv').prop('title', 'Connected');
    }
    else {
        $('#lnkStartEndConv').css('color', 'darkgrey');
        $('#lnkStartEndConv').prop('title', 'Disconnected');
    }
}

function fnStartConversation() {
    Watermark = -1;
    //Start Conversation
    $.ajax({
        type: "POST",
        url: "https://directline.botframework.com/v3/directline/conversations",
        cache: false,
        async: true,
        headers: {
            'Authorization': 'Bearer ' + DirectLineSecret
        },
        success: function (data) {
            ConversationID = data.conversationId;
            console.debug(ConversationID);
            isConversationStarted = true;
            fnToggleElement(isConversationStarted);
            //if (!isMaximized) {
            //    $('.btnMinimize').click();
            //}            
            //$(".alert-success").text('User is connected');
            //$(".alert-success").css('display', 'inline');
            //window.setTimeout(function () { $(".alert-success").css('display', 'none'); }, 2000);
            output += 'Start Conversation  ';
        },
        error: function (err) {            
            isConversationStarted = false;
            fnToggleElement(isConversationStarted);
            output += 'Error occurred while starting conversation  ';
        }
    });
}

function fnEndConversation() {
    if (ConversationID != '') {
        //End of Conversation
        $.ajax({
            type: "POST",
            url: "https://directline.botframework.com/v3/directline/conversations/" + ConversationID + "/activities",
            cache: false,
            async: true,
            headers: {
                'Authorization': 'Bearer ' + DirectLineSecret,
                'Content-Type': 'application/json'
            },
            data: JSON.stringify({
                "type": "endOfConversation",
                "from": {
                    "id": "user1"
                }
            }),
            success: function (data) {
                isConversationStarted = false;
                fnToggleElement(isConversationStarted);
                $('#lnkClear').click();
                //if (isMaximized) {
                //    $('.btnMinimize').click();
                //}
                //$(".alert-success").text('User is disconnected');
                //$(".alert-success").css('display', 'inline');
                //window.setTimeout(function () { $(".alert-success").css('display', 'none'); $('#lnkClear').click(); }, 2000);
                output += 'EOC ';                
            },
            error: function (err) {                
                isConversationStarted = true;
                fnToggleElement(isConversationStarted);
                output += 'Error occurred while ending Conversation ';
            }
        });
    }
    else {
        alert('Start the conversation first');
    }
}

function fnSendMessage(ConversationID, input) {
    addChat('User', getUserFormattedInput(input));
    console.debug('Send ' + ConversationID);
    //Send Message
    return $.ajax({
        type: "POST",
        url: "https://directline.botframework.com/v3/directline/conversations/" + ConversationID + "/activities",
        cache: false,
        async: true,
        headers: {
            'Authorization': 'Bearer ' + DirectLineSecret,
            'Content-Type': 'application/json'
        },
        data: JSON.stringify({
            "type": "message",
            "from": {
                "id": "user1"
            },
            "text": input
        }),
        success: function (data) {
            DataID = data.id;
            output += 'Send Message  ';
        },
        error: function (err) {
            output += 'Error occurred while sending message  ';
        }
    });
}

function fnReceiveMessage(ConversationID) {
    addChat('Spinner', '<img src="/Content/loader-new.gif" height=30 width=30 title="Receiving message..." style="float:left"></img>');
    var ButtonsHTML = '';
    console.debug('Receive ' + ConversationID);
    //Receive Message
    return $.ajax({
        type: "GET",
        url: "https://directline.botframework.com/v3/directline/conversations/" + ConversationID + "/activities?watermark=" + (++Watermark),
        cache: false,
        async: true,
        headers: {
            'Authorization': 'Bearer ' + DirectLineSecret
        },
        success: function (data) {
            $('.ChatArea .TimeStamp:last-child').remove();
            $('.ChatArea img:last-child').remove();
            CardHTML = CardOriginalHTML;
            var activity = data.activities;
            for (var i = 0; i < activity.length; i++) {
                if (activity[i].from.id == 'ProformaChatBotHandle') {
                    var attachment = activity[i].attachments;
                    if (activity[i].conversation.id == ConversationID) {
                        if (attachment.length == 0) {
                            addChat('Bot', getUserFormattedInput(activity[i].text).replace(/\n\n/g, '\n').replace(/\n/g, '<br />'));
                        }
                        else {
                            CardHTML = CardHTML.replace('@', attachment[0].content.title == undefined ? '' : attachment[0].content.title);
                            CardHTML = CardHTML.replace('#', attachment[0].content.text);
                            if (attachment[0].content.buttons.length > 0) {                               
                                var clr_counter = Math.floor(Math.random() * 15);
                                for (var j = 0; j < attachment[0].content.buttons.length; j++) {
                                    if (attachment[0].content.buttons[j].value.toLowerCase() == 'yes') {
                                        ButtonsHTML += '<button class="btnCard1 colorYes">Yes. I\'m lovin it.</button>';
                                    }
                                    else if (attachment[0].content.buttons[j].value.toLowerCase() == 'no') {
                                        ButtonsHTML += '<button class="btnCard1 colorNo">No. Maybe later.</button>';
                                    }
                                    else {
                                        if (clr_counter <= 15) {                                            
                                            ButtonsHTML += '<button class="btnCard ' + colorArr[clr_counter - 1] + '" title="' + attachment[0].content.buttons[j].value + '">' + attachment[0].content.buttons[j].value + '</button>';
                                            clr_counter++;
                                            if (clr_counter == 16) {
                                                clr_counter = 1;
                                            }
                                        }                                        
                                    }
                                }                                
                            }
                            CardHTML = CardHTML.replace('$', ButtonsHTML);
                            addChat('Bot', CardHTML);
                        }
                        output += 'Receive Message  ';
                        Watermark = data.watermark;
                    }
                }
            }
        },
        error: function (err) {
            output += 'Error occurred while receiving message  ';
        }
    });
}