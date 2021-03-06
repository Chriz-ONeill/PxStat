/*******************************************************************************
Custom JS application specific
*******************************************************************************/

//#region Add Namespace
app.release = app.release || {};

app.release.comment = {};
app.release.comment.validation = {};
app.release.comment.ajax = {};
app.release.comment.callback = {};
//#endregion

//#region Read

/**
* 
* @param {*} data
*/
app.release.comment.render = function (data) {
    if (app.release.isModerator || app.release.isHistorical) {
        $("#release-comment [name=create]").prop("disabled", true);
        $("#release-comment [name=update]").prop("disabled", true);
        $("#release-comment [name=delete]").prop("disabled", true);
    } else {
        $("#release-comment [name=create]").prop("disabled", false);
        $("#release-comment [name=update]").prop("disabled", false);
        $("#release-comment [name=delete]").prop("disabled", false);
    }

    // Update
    if (data.CmmValue) {
        app.release.comment.validation.update();
        $("#release-comment [name=create]").hide();
        $("#release-comment [name=update]").show();

        $("#release-comment [name=delete]").prop("disabled", false);

        var tinyMceId = $("#release-comment-modal-update [name=cmm-value]").attr("id");
        tinymce.get(tinyMceId).setContent(data.CmmValue);

        $("#release-comment [name=cmm-value]").empty().html(app.library.html.parseBbCode(data.CmmValue)).show();
    }
    // Create
    else {
        app.release.comment.validation.create();
        $("#release-comment [name=create]").show();
        $("#release-comment [name=update]").hide();

        $("#release-comment [name=delete]").prop("disabled", true);

        var tinyMceId = $("#release-comment-modal-create [name=cmm-value]").attr("id");
        tinymce.get(tinyMceId).setContent("");

        $("#release-comment [name=cmm-value]").empty().hide();
    }

    $("#release-comment").hide().fadeIn();
};
//#endregion

//#region Create
/**
* 
*/
app.release.comment.create = function () {
    $("#release-comment-modal-create").modal("show");
};

/**
*
*/
app.release.comment.validation.create = function () {
    $("#release-comment-modal-create form").trigger("reset").onSanitiseForm().validate({
        ignore: [],
        rules: {
            "cmm-value": {
                required: function (element) {
                    tinymce.triggerSave();
                    return true;
                }
            }
        },
        errorPlacement: function (error, element) {
            $("#release-comment-modal-create [name=" + element[0].name + "-error-holder]").append(error[0]);
        },
        submitHandler: function (form) {
            $(form).sanitiseForm();
            app.release.comment.ajax.create();
        }
    }).resetForm();
};

/**
* 
*/
app.release.comment.ajax.create = function () {
    var tinyMceId = $("#release-comment-modal-create").find("[name=cmm-value]").attr("id");
    var CmmValue = tinymce.get(tinyMceId).getContent();

    api.ajax.jsonrpc.request(
        app.config.url.api.private,
        "PxStat.Data.Release_API.UpdateComment",
        { "RlsCode": app.release.RlsCode, "CmmValue": CmmValue },
        "app.release.comment.callback.create",
        null,
        null,
        null,
        { async: false });
};

/**
* 
* @param {*} data
*/
app.release.comment.callback.create = function (data) {
    if (data == C_APP_API_SUCCESS) {
        api.modal.success(app.library.html.parseDynamicLabel("success-record-added", [""]));
        $("#release-comment-modal-create").modal("hide");
        app.release.ajax.read();
    }
    // Handle Exception
    else api.modal.exception(app.label.static["api-ajax-exception"]);
};
//#endregion

//#region Update

/**
* 
*/
app.release.comment.update = function () {
    $("#release-comment-modal-update").modal("show");
};

/**
* 
*/
app.release.comment.validation.update = function () {
    $("#release-comment-modal-update form").trigger("reset").onSanitiseForm().validate({
        ignore: [],
        rules: {
            "cmm-value": {
                required: function (element) {
                    tinymce.triggerSave();
                    return true;
                }
            }
        },
        errorPlacement: function (error, element) {
            $("#release-comment-modal-update [name=" + element[0].name + "-error-holder]").append(error[0]);
        },
        submitHandler: function (form) {
            $(form).sanitiseForm();
            app.release.comment.ajax.update();
        }
    }).resetForm();
};

/**
* 
*/
app.release.comment.ajax.update = function () {
    var tinyMceId = $("#release-comment-modal-update [name=cmm-value]").attr("id");
    var CmmValue = tinymce.get(tinyMceId).getContent();

    api.ajax.jsonrpc.request(
        app.config.url.api.private,
        "PxStat.Data.Release_API.UpdateComment",
        { "RlsCode": app.release.RlsCode, "CmmValue": CmmValue },
        "app.release.comment.callback.update",
        null,
        null,
        null,
        { async: false });
};

/**
* 
* @param {*} data
*/
app.release.comment.callback.update = function (data) {
    if (data == C_APP_API_SUCCESS) {
        api.modal.success(app.library.html.parseDynamicLabel("success-record-updated", [""]));
        $("#release-comment-modal-update").modal("hide");
        app.release.ajax.read();
    }
    // Handle Exception
    else api.modal.exception(app.label.static["api-ajax-exception"]);
};
//#endregion

//#region Delete

/**
* 
*/
app.release.comment.delete = function () {
    api.modal.confirm(app.library.html.parseDynamicLabel("confirm-delete", [""]), app.release.comment.ajax.delete);
};

/**
* 
*/
app.release.comment.ajax.delete = function () {
    api.ajax.jsonrpc.request(
        app.config.url.api.private,
        "PxStat.Data.Release_API.DeleteComment",
        { "RlsCode": app.release.RlsCode },
        "app.release.comment.callback.delete",
        null,
        null,
        null,
        { async: false });
};

/**
* 
* @param {*} data
*/
app.release.comment.callback.delete = function (data) {
    if (data == C_APP_API_SUCCESS) {
        api.modal.success(app.library.html.parseDynamicLabel("success-record-deleted", [""]));
        app.release.ajax.read();
    }
    // Handle Exception
    else api.modal.exception(app.label.static["api-ajax-exception"]);
};
//#endregion