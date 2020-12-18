namespace ServerSpa.Pages

open Microsoft.AspNetCore.Http

open Giraffe
open Feliz.ViewEngine

open Saturn.Auth

open type Feliz.ViewEngine.prop

open FSharp.Control.Tasks

open ServerSpa
open ServerSpa.Components
open Microsoft.AspNetCore.Antiforgery

[<RequireQualifiedAccess>]
module Profile =
    let private infoPartial =
        let cardHeader = CardHeader "My Profile" None |> Some
        let cardContent = Html.div [ id "#content"; children [] ]

        let footer =
            CardActionsFooter [
                Html.a [
                    custom ("hx-get", "/profile/edit")
                    custom ("hx-swap", "outerHTML")
                    custom ("hx-target", "#infopartial")
                    className "card-footer-item"
                    text "Edit"
                ]
            ]
            |> Some

        Html.article [
            id "infopartial"
            children [
                CustomCard cardContent cardHeader footer
            ]
        ]

    let Index =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            let html =
                let content =
                    Html.div [
                        className "container"
                        children [ infoPartial ]
                    ]

                Layouts.Default content

            Helpers.htmx html next ctx

    let UserInfoPartial =
        fun (next: HttpFunc) (ctx: HttpContext) -> task { return! Helpers.htmx infoPartial next ctx }

    let EditUserInfoPartial =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            task {
                let antiforgery = ctx.GetService<IAntiforgery>()

                let html =
                    let cardHeader = CardHeader "My Profile" None |> Some

                    let cardContent =
                        Html.div [
                            children [
                                Html.form [
                                    id "editform"
                                    children [
                                        Html.input [
                                            type' "text"
                                            name "name"
                                            id "name"
                                        ]
                                    ]
                                ]
                            ]
                        ]

                    let footer =
                        CardActionsFooter [
                            Html.a [
                                custom ("hx-post", "/profile/save")
                                custom ("hx-swap", "outerHTML")
                                custom ("hx-include", "#editform")
                                custom ("hx-target", "#editpartial")
                                className "card-footer-item"
                                text "Save"
                            ]
                        ]
                        |> Some

                    Html.article [
                        id "editpartial"
                        children [
                            Helpers.csrfInputWithSideEffects antiforgery ctx
                            CustomCard cardContent cardHeader footer
                        ]
                    ]

                return! Helpers.htmx html next ctx
            }
