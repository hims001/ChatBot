var output = '';
var DirectLineSecret = '';
var ConversationID = '';
var Token;
var DataID;
var Watermark = -1;
var CardOriginalHTML = '<div class="dvCard" style="border:1px solid black;width:200px">' +
    '<div class="dvCardTitle" style="font-weight:bold;">@</div>' +
    '<div class="dvCardText">#</div>' +
    '<div class="dvCardButtons">$</div>' +
'</div>'
var CardHTML = CardOriginalHTML;

$('#btnStart').click(function () {
    //Start Conversation
    $.ajax({
        type: "POST",
        url: "https://directline.botframework.com/v3/directline/conversations",
        cache: false,
        async: false,
        headers: {
            'Authorization': 'Bearer ' + DirectLineSecret
        },
        success: function (data) {
            ConversationID = data.conversationId;
            $('body').append('<span>' + ConversationID + '</span><br/>');
            output += 'Start Conversation  ';
        },
        error: function (err) {
            output += 'Error occurred while starting conversation  ';
        }
    });
});

$('#btnEnd').click(function () {
    if (ConversationID != '') {
        //End of Conversation
        $.ajax({
            type: "POST",
            url: "https://directline.botframework.com/v3/directline/conversations/" + ConversationID + "/activities",
            cache: false,
            async: false,
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
                $('body').append('<span>' + ConversationID + '</span><br/>');
                output += 'EOC ';
            },
            error: function (err) {
                output += 'Error occurred while ending Conversation ';
            }
        });
    }
    else {
        alert('Start the conversation first');
    }
});

$('#btn').click(function () {
    var ReceiveHandle;
    if (ConversationID != '') {
        var SendHandle = fnSendMessage(ConversationID, $('#txtInput').val());
        SendHandle.done(function () {
            ReceiveHandle = fnReceiveMessage(ConversationID);
        });
    }
    else {
        alert('Start the conversation first');
    }
});


//alert(output);


//Token Generation
//$.ajax({
//    type: "POST",
//    url: "https://directline.botframework.com/v3/directline/tokens/generate",
//    cache: false,
//    async: false,
//    beforeSend: function (xhr) {
//        /* Authorization header */
//        xhr.setRequestHeader("Authorization", "Bearer " + DirectLineSecret);
//    },
//    success: function (data) {
//        ConversationID = data.conversationId;
//        Token = data.token;
//        output += '1. Token generated  ';
//    },
//    error: function (err) {
//        output += '1. Error occurred while generating token  ';
//    }
//});