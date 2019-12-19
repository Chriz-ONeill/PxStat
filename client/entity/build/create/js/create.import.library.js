/*******************************************************************************
Custom JS application specific
*******************************************************************************/
//#region Add Namespace
app.build = app.build || {};
app.build.create.import = {};
app.build.create.import.ajax = {};
app.build.create.import.callback = {};
app.build.create.import.validate = {};
app.build.create.import.validate.ajax = {};
app.build.create.import.validate.callback = {};
//#endregion


/**
 *
 */
app.build.create.import.reset = function () {
    //clean up modal
    $("#build-create-import").find("[name=frequency-codes]").hide();
    $("#build-create-import").find("[name=frequency-warning]").hide();
    $("#build-create-import").find("[name=build-create-import-file]").val("");
    $("#build-create-import").find("[name=upload-file-name]").empty().hide();
    $("#build-create-import").find("[name=upload-file-tip]").show();
    $("#build-create-import").find("[name=upload-error-card]").hide();

    // Disable Parse Button
    $("#build-create-import").find("[name=parse-source-file]").prop("disabled", true);

    //clean up namespaced variables
    app.build.create.file.import.FrqCode = null;
    app.build.create.file.import.FrqValue = null;
    app.build.create.file.import.Signature = null;
    app.build.create.file.import.content.UTF8 = null;
    app.build.create.file.import.content.Base64 = null;
};

/**
 *
 */
app.build.create.import.validate.ajax.read = function () {

    app.build.create.file.import.FrqCode = $("#build-create-import").find("[name=frq-code]").val();
    app.build.create.file.import.FrqValue = $("#build-create-import").find("[name=frq-value]:checked").val() || null;

    api.ajax.jsonrpc.request(
        app.config.url.api.private,
        "PxStat.Build.Build_API.Validate",
        {
            "FrqCodeTimeval": app.build.create.file.import.FrqCode,
            "FrqValueTimeval": app.build.create.file.import.FrqValue,
            "MtrInput": app.build.create.file.import.content.Base64
        },
        "app.build.create.import.validate.callback.read",
        null,
        null,
        null,
        {
            async: false,
            timeout: app.config.upload.timeout
        }
    );
    // Add the progress bar
    api.spinner.progress.start(api.spinner.progress.getTimeout(app.build.create.file.import.content.Base64.length, app.config.upload.unitsPerSecond.read));
};

app.build.create.import.validate.callback.read = function (response) {
    if (response.error) {
        var errorOutput = $("<ul>", {
            class: "list-group"
        });
        if (Array.isArray(response.error.message)) {
            $.each(response.error.message, function (_index, value) {
                var error = $("<li>", {
                    class: "list-group-item",
                    html: value.ErrorMessage
                });
                errorOutput.append(error);
            });
        } else {
            var error = $("<li>", {
                class: "list-group-item",
                html: response.error.message
            });
            errorOutput.append(error);
        }
        api.modal.error(errorOutput);
    } else if (response.data) {
        if (response.data.Signature) {
            // Store for later use
            app.build.create.file.import.Signature = response.data.Signature;
            app.build.create.import.ajax.read();
        }
        else {
            $("#build-create-import").find("[name=frequency-codes]").show();
            $("#build-create-import").find("[name=frequency-warning]").show();
            // Populate the Frequency list
            $("#build-create-import").find("[name=frequency-radio-group]").empty();

            $.each(response.data.FrqValueCandidate, function (key, value) {
                $("#build-create-import").find("[name=frequency-radio-group]").append(function () {
                    return $("<li>", {
                        "class": "list-group-item",
                        "html": $("<input>", {
                            "type": "radio",
                            "name": "frq-value",
                            "value": value
                        }).get(0).outerHTML + " " + value
                    }).get(0).outerHTML;
                });
            });
            // Run validation
            // app.build.update.validate.frequencyModal();

        }
    }
    else api.modal.exception(app.label.static["api-ajax-exception"]);

};


app.build.create.import.ajax.read = function () {
    api.ajax.jsonrpc.request(
        app.config.url.api.private,
        "PxStat.Build.Build_API.Read",
        {
            "FrqCodeTimeval": app.build.create.file.import.FrqCode,
            "FrqValueTimeval": app.build.create.file.import.FrqValue,
            "MtrInput": app.build.create.file.import.content.Base64,
            "Signature": app.build.create.file.import.Signature
        },
        "app.build.create.import.callback.read",
        null,
        null,
        null,
        { async: false });
};

app.build.create.import.callback.read = function (response) {
    //put JSON-stat into namespace variable for future use    
    if (response.error) {
        var errorOutput = $("<ul>", {
            class: "list-group"
        });
        if (Array.isArray(response.error.message)) {
            $.each(response.error.message, function (_index, value) {
                var error = $("<li>", {
                    class: "list-group-item",
                    html: value.ErrorMessage
                });
                errorOutput.append(error);
            });
        } else {
            var error = $("<li>", {
                class: "list-group-item",
                html: response.error.message
            });
            errorOutput.append(error);
        }
        api.modal.error(errorOutput);

        $("#build-create-import").find("[name=upload-error]").html(errorOutput.get(0).outerHTML);
        $("#build-create-import").find("[name=upload-error-card]").fadeIn();
    }

    else if (!response.data || (Array.isArray(response.data) && !response.data.length)) {
        api.modal.information(app.label.static["api-ajax-nodata"]);
        // Do nothing 
    }
    else if (response.data !== undefined) {
        $("#build-create-import").modal("hide");
        app.build.create.file.import.content.JsonStat = [];
        //parse each JSON-stat response and push to namespace varaiable
        $.each(response.data, function (index, data) {
            app.build.create.file.import.content.JsonStat.push(JSONstat(data))
        });
        app.build.create.import.callback.read.drawProperties();

    }
    else api.modal.exception(app.label.static["api-ajax-exception"]);
};

app.build.create.import.callback.read.drawProperties = function () {
    //get matrix code
    var defaultData = app.build.create.file.import.content.JsonStat[0];

    var mrtValue = defaultData.extension.matrix;
    $("#build-create-initiate-setup").find("[name=mtr-value]").val(mrtValue);

    //get and set the frequency code
    var frqCode = null;
    for (i = 0; i < defaultData.length; i++) {
        if (defaultData.Dimension(i).role == "time") {
            frqCode = defaultData.id[i];
        }
    };

    $("#build-create-initiate-setup [name=frequency-code] > option").each(function () {
        if (this.value == frqCode) {
            $(this).attr("selected", "selected");
        }
        else {
            $(this).removeAttr("selected");
        }
    });

    //get and set the copyright code
    var cprCode = defaultData.extension.copyright.code;

    $("#build-create-initiate-setup [name=copyright-code] > option").each(function () {
        if (this.value == cprCode) {
            $(this).attr("selected", "selected");
        }
        else {
            $(this).removeAttr("selected");
        }
    });

    //get and set the official flag
    if (defaultData.extension.official) {
        $("#build-create-initiate-setup [name=official-flag]").bootstrapToggle('on');
    }
    else {
        $("#build-create-initiate-setup [name=official-flag]").bootstrapToggle('off');
    }

    //set languages
    var importLanguages = [];
    $.each(app.build.create.file.import.content.JsonStat, function (index, data) {
        importLanguages.push(data.extension.language.code)
    });
    $("[name=lng-group]").each(function () {
        if (jQuery.inArray($(this).attr("value"), importLanguages) > -1) {
            $(this).prop("checked", true);
        }
        else {
            $(this).prop("checked", false);
        }
    });
}
