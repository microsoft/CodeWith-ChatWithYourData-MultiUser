import { makeStyles, tokens, Skeleton, SkeletonItem } from '@fluentui/react-components';

const useClasses = makeStyles({
    skeletonItem: {
        boxSizing: 'border-box',
        width: '100%',
        marginTop: tokens.spacingVerticalS,
        padding: tokens.spacingHorizontalXS,
    },
});

export function ListSkeleton() {
    const classes = useClasses();

    return (
        <Skeleton aria-label="Loading..">
            <SkeletonItem size={32} shape="rectangle" className={classes.skeletonItem}/>
            <SkeletonItem size={32} shape="rectangle" className={classes.skeletonItem}/>
            <SkeletonItem size={32} shape="rectangle" className={classes.skeletonItem}/>
        </Skeleton>
    );
};