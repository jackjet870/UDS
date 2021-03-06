﻿<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="editbulletin.aspx.cs" Inherits="UDS.SubModule.bulletin.editbulletin" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>编辑公告</title>
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
	<link rel="stylesheet" href="../../Css/bootstrap.min.css"/>
	<link rel="stylesheet" href="../../Css/bootstrap-responsive.min.css" />
	<link rel="stylesheet" href="../../Css/font-awesome.min.css" />
	<!--[if IE 7]>
		<link rel="stylesheet" href="../../Css/font-awesome-ie7.min.css" />
	<![endif]-->
    <link rel="stylesheet" href="../../Css/quantumcode.css" />
    <link rel="stylesheet" href="../../Css/quantumcode-resposive.css" />
    <link rel="stylesheet" href="../../Css/quantumcode-skins.css" />

    <!--[if lt IE 9]>
		<link rel="stylesheet" href="../../Css/quantumcode-ie.css" />
	<![endif]-->
    <link type="text/css" rel="stylesheet" href="../../Css/fineuploader-3.5.0.css" />

    <style type="text/css">
		.widget-box .widget-toolbar a
		{
			color: white;
		}
	</style>
</head>
<body>
    <div class="container-fluid">
        <div id="page-content">
		    <div class="page-header">
			    <h1 id="pageheader">公告管理</h1>
		    </div>
            <div class="row-fluid">
                <div class="span12">
                    <div class="widget-box">
                        <div class="widget-header header-color-blue">
                            <h5><i class="icon-bullhorn icon-2x"></i>公告</h5>
                            <div class="widget-toolbar">
                                <!--<a href='javascript:'><i class="icon-edit"></i>新建公告</a>-->
                                <!--<a href="#"><i class="icon-remove"></i>删除</a>-->
                            </div>
                        </div>
                        <div class="widget-body">
							<div class="widget-body-inner" style="display: block">
								<div class="widget-main no-padding">
                                    <form class="well-large form-horizontal">
                                        <div class="control-group">
                                            <label class="control-label" for="txtSubject">标题</label>
                                            <input type="text" id="txtSubject" placeholder="标题" />
                                            <div id="btnSave" class="btn btn-primary btn-small">发布公告</div>
                                        </div>
                                    </form>
                                    <div id="fine-uploader"></div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
	</div>
    <script type="text/javascript" src="../../js/jquery-1.9.1.min.js"></script>
    <script type="text/javascript" src="../../ckeditor/ckeditor.js"></script>
    <script type="text/javascript" src="../../js/underscore-min.js"></script>
    <script type="text/javascript" src="../../js/backbone-min.js"></script>
    <script type="text/javascript" src="../../js/jquery.fineuploader-3.5.0.min.js"></script>
    <script type="text/javascript" src="../../js/iframe.xss.response-3.5.0.js"></script>
    <script type="text/javascript">
        var uuid = '';

        (function ($) {
            $.getUrlParam = function (name) {
                var reg = new RegExp("(^|&)" + name + "=([^&]*)(&|$)");
                var r = window.location.search.substr(1).match(reg);
                if (r != null) return unescape(r[2]); return null;
            }
        })(jQuery);

        $("#btnSave").bind("click", null,
            function () {
                var title = $("#txtSubject").val();

                if (null != title && "" != title) {
                    $.ajax({
                        url: "bulletinAction.aspx",
                        type: "POST",
                        data: "t=" + title + "&uuid=" + uuid,
                        dataType: "text",
                        success: function (data, textStatus, jqXHR) {
                            window.location.href = "index.html";
                        },
                        error: function (jqXHR, textStatus, errorThrown) {
                            alert(jqXHR.responseText);
                        }
                    });
                }
                else {
                    alert("必须输入标题");
                    $("#txtSubject").focus();
                }
            });

        $(document).ready(function () {
            var isNew = $.getUrlParam("isnew");

            if (null != isNew || "" != isNew) {
                $("#pageheader").text("新建公告");
            }

            $.get("bulletinAction.aspx?m=uuid",
                null,
                function (data, textStatus, jqXHR) {
                    uuid = data;
                },
                "text").done(function () {
                    $('#fine-uploader').fineUploader({
                        text: {
                            uploadButton: "上传公告文件"
                        },
                        display: {
                            fileSizeOnSubmit: true
                        },
                        request: {
                            endpoint: '../upload/doUpload.aspx?uuid=' + uuid
                        }
                    })
                });
        });
    </script>
</body>
</html>
