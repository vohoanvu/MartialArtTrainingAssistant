import {ReactElement} from "react";
import {useTranslation} from "react-i18next";
import { PageWrapper } from "@/components/zen/PageWrapper";
import { SectionHeader } from "@/components/zen/SectionHeader";

//Unused component
export default function About(): ReactElement {
    const { t } = useTranslation();

    return (
        <PageWrapper>
            <SectionHeader title={t("about.title")} />
            <p className="mb-4 font-sans text-ink-400">{t("about.description")}</p>

            <h2 className="font-serif text-3xl font-bold mb-3 text-ink-400">{t("about.featuresTitle")}</h2>
            <ul className="list-disc list-inside mb-4 font-sans text-ink-400">
                <li>{t("home.features.ease.description")}</li>
                <li>{t("home.features.consistency.description")}</li>
                <li>{t("home.features.npm.description")}</li>
            </ul>

            <h2 className="font-serif text-3xl font-bold mb-3 text-ink-400">{t("about.techStackTitle")}</h2>
            <p className="mb-4 font-sans text-ink-400">.Net 8.0, React, Docker, Nginx, PostgreSQL, Tailwind CSS, Zustand, i18next</p>

            <h2 className="font-serif text-3xl font-bold mb-3 text-ink-400">{t("about.authorTitle")}</h2>
            <p className="text-ink-400">{t("about.authorName")}</p>
            <p><a href="https://github.com/SirCypkowskyy" className="text-samurai-400 hover:text-samurai-500">{t("about.githubProfile")}</a></p>
            <p><a href="mailto:dcyprian.a.gburek@gmail.com" className="text-samurai-400 hover:text-samurai-500">{t("about.email")}: dcyprian.a.gburek@gmail.com</a></p>
        </PageWrapper>
    );
}